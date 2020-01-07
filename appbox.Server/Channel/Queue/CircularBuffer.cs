using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace appbox.Server
{
    //Copyright https://github.com/spazzarama/SharedMemory

    /// <summary>
    /// A lock-free FIFO shared memory circular buffer (or ring buffer) utilising a <see cref="MemoryMappedFile"/>.
    /// </summary>
    public unsafe class CircularBuffer : SharedBuffer
    {
        #region Public/Protected properties

        /// <summary>
        /// The number of nodes within the circular linked-list
        /// </summary>
        public int NodeCount { get; private set; }

        /// <summary>
        /// The buffer size of each node
        /// </summary>
        public int NodeBufferSize { get; private set; }

#if Linux
        /// <summary>
        /// Event signaled when data has been written if the reading index has caught up to the writing index
        /// </summary>
        protected PosixSemaphore DataExists { get; set; }

        /// <summary>
        /// Event signaled when a node becomes available after reading if the writing index has caught up to the reading index
        /// </summary>
        protected PosixSemaphore NodeAvailable { get; set; }
#else
        /// <summary>
        /// Event signaled when data has been written if the reading index has caught up to the writing index
        /// </summary>
        protected EventWaitHandle DataExists { get; set; }

        /// <summary>
        /// Event signaled when a node becomes available after reading if the writing index has caught up to the reading index
        /// </summary>
        protected EventWaitHandle NodeAvailable { get; set; }
#endif

        /// <summary>
        /// The offset relative to <see cref="SharedBuffer.BufferStartPtr"/> where the node header starts within the buffer region of the shared memory
        /// </summary>
        protected virtual long NodeHeaderOffset => 0;

        /// <summary>
        /// Where the linked-list nodes are located within the buffer
        /// </summary>
        protected virtual long NodeOffset => NodeHeaderOffset + Marshal.SizeOf(typeof(NodeHeader));

        /// <summary>
        /// Where the list of buffers are located within the shared memory
        /// </summary>
        protected virtual long NodeBufferOffset => NodeOffset + (Marshal.SizeOf(typeof(Node)) * NodeCount);

        /// <summary>
        /// Provide direct access to the Node[] memory
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        protected virtual Node* this[int i]
        {
            get
            {
                if (i < 0 || i >= NodeCount)
                    throw new ArgumentOutOfRangeException();

                return ((Node*)(BufferStartPtr + NodeOffset)) + i;
            }
        }

#endregion

#region Private field members

        private NodeHeader* _nodeHeader = null;

#endregion

#region Structures

        /// <summary>
        /// Provides cursors for the circular buffer along with dimensions
        /// </summary>
        /// <remarks>This structure is the same size on 32-bit and 64-bit architectures.</remarks>
        [StructLayout(LayoutKind.Sequential)]
        public struct NodeHeader
        {
            /// <summary>
            /// The index of the first unreadable node
            /// </summary>
            public volatile int ReadEnd;
            /// <summary>
            /// The index of the next readable node
            /// </summary>
            public volatile int ReadStart;

            /// <summary>
            /// The index of the first unwritable node
            /// </summary>
            public volatile int WriteEnd;
            /// <summary>
            /// The index of the next writable node
            /// </summary>
            public volatile int WriteStart;

            public volatile int WriteWaits;
            public volatile int ReadWaits;

            /// <summary>
            /// The number of nodes within the buffer
            /// </summary>
            public int NodeCount;

            /// <summary>
            /// The size of the buffer for each node
            /// </summary>
            public int NodeBufferSize;
        }

        /// <summary>
        /// Represents a node within the buffer's circular linked list
        /// </summary>
        /// <remarks>This structure is the same size on 32-bit and 64-bit architectures.</remarks>
        [StructLayout(LayoutKind.Sequential)]
        public struct Node
        {
            /// <summary>
            /// The previous node.
            /// </summary>
            public int Next;

            /// <summary>
            /// The next node.
            /// </summary>
            public int Prev;

            /// <summary>
            /// A flag used while returning a node for writing after having been read.
            /// </summary>
            public volatile int DoneRead;

            /// <summary>
            /// A flag used while posting a node for reading after writing is completed.
            /// </summary>
            public volatile int DoneWrite;

            /// <summary>
            /// Represents the offset relative to <see cref="SharedBuffer.BufferStartPtr"/> where the data for this node can be found.
            /// </summary>
            public long Offset;

            /// <summary>
            /// Represents the index of the current node.
            /// </summary>
            public int Index;

            /// <summary>
            /// Holds the number of bytes written into this node.
            /// </summary>
            public int AmountWritten;
        }

#endregion

#region Constructors

        /// <summary>
        /// Creates and opens a new <see cref="CircularBuffer"/> instance with the specified name, node count and buffer size per node.
        /// </summary>
        /// <param name="name">The name of the shared memory to be created</param>
        /// <param name="nodeCount">The number of nodes within the circular linked-list (minimum of 2)</param>
        /// <param name="nodeBufferSize">The buffer size per node in bytes. The total shared memory size will be <code>Marshal.SizeOf(SharedMemory.SharedHeader) + Marshal.SizeOf(CircularBuffer.NodeHeader) + (Marshal.SizeOf(CircularBuffer.Node) * nodeCount) + (bufferSize * nodeCount)</code></param>
        /// <remarks>
        /// <para>The maximum total shared memory size is dependent upon the system and current memory fragmentation.</para>
        /// <para>The shared memory layout on 32-bit and 64-bit architectures is:<br />
        /// <code>
        /// |       Header       |   NodeHeader  | Node[0] | ... | Node[N-1] | buffer[0] | ... | buffer[N-1] |<br />
        /// |      16-bytes      |    24-bytes   |       32-bytes * N        |     NodeBufferSize * N        |<br />
        ///                      |------------------------------BufferSize-----------------------------------|<br />
        /// |-----------------------------------------SharedMemorySize---------------------------------------|
        /// </code>
        /// </para>
        /// </remarks>
        public CircularBuffer(string name, int nodeCount, int nodeBufferSize)
            : this(name, nodeCount, nodeBufferSize, true)
        {
            Open();
        }

        /// <summary>
        /// Opens an existing <see cref="CircularBuffer"/> with the specified name.
        /// </summary>
        /// <param name="name">The name of an existing <see cref="CircularBuffer"/> previously created with <see cref="SharedBuffer.IsOwnerOfSharedMemory"/>=true</param>
        public CircularBuffer(string name)
            : this(name, 0, 0, false)
        {
            Open();
        }

        private CircularBuffer(string name, int nodeCount, int nodeBufferSize, bool ownsSharedMemory)
            : base(name, Marshal.SizeOf(typeof(NodeHeader)) + (Marshal.SizeOf(typeof(Node)) * nodeCount) + (nodeCount * (long)nodeBufferSize), ownsSharedMemory)
        {
#region Argument validation
            if (ownsSharedMemory && nodeCount < 2)
                throw new ArgumentOutOfRangeException(nameof(nodeCount), nodeCount, "The node count must be a minimum of 2.");
#if DEBUG
            else if (!ownsSharedMemory && (nodeCount != 0 || nodeBufferSize > 0))
                System.Diagnostics.Debug.Write("Node count and nodeBufferSize are ignored when opening an existing shared memory circular buffer.", "Warning");
#endif
#endregion

            if (IsOwnerOfSharedMemory)
            {
                NodeCount = nodeCount;
                NodeBufferSize = nodeBufferSize;
            }
        }

#endregion

#region Open / Close

        /// <summary>
        /// Attempts to create the <see cref="EventWaitHandle"/> handles and initialise the node header and buffers.
        /// </summary>
        /// <returns>True if the events and nodes were initialised successfully.</returns>
        protected override bool DoOpen()
        {
            // Create signal events
#if Linux
            if (IsOwnerOfSharedMemory)
            {
                DataExists = PosixSemaphore.Create(Name + "_evt_dataexists");
                NodeAvailable = PosixSemaphore.Create(Name + "_evt_nodeavail");
            }
            else
            {
                DataExists = PosixSemaphore.Open(Name + "_evt_dataexists");
                NodeAvailable = PosixSemaphore.Open(Name + "_evt_nodeavail");
            }
#else
            DataExists = new EventWaitHandle(false, EventResetMode.AutoReset, Name + "_evt_dataexists");
            NodeAvailable = new EventWaitHandle(false, EventResetMode.AutoReset, Name + "_evt_nodeavail");
#endif

            if (IsOwnerOfSharedMemory)
            {
                // Retrieve pointer to node header
                _nodeHeader = (NodeHeader*)(BufferStartPtr + NodeHeaderOffset);

                // Initialise the node header
                InitialiseNodeHeader();

                // Initialise nodes entries
                InitialiseLinkedListNodes();
            }
            else
            {
                // Load the NodeHeader
                _nodeHeader = (NodeHeader*)(BufferStartPtr + NodeHeaderOffset);
                NodeCount = _nodeHeader->NodeCount;
                NodeBufferSize = _nodeHeader->NodeBufferSize;
            }

            return true;
        }

        /// <summary>
        /// Initialises the node header within the shared memory. Only applicable if <see cref="SharedBuffer.IsOwnerOfSharedMemory"/> is true.
        /// </summary>
        private void InitialiseNodeHeader()
        {
            if (!IsOwnerOfSharedMemory)
                return;

            NodeHeader header = new NodeHeader();
            header.ReadStart = 0;
            header.ReadEnd = 0;
            header.WriteEnd = 0;
            header.WriteStart = 0;
            header.NodeBufferSize = NodeBufferSize;
            header.NodeCount = NodeCount;
            base.Write<NodeHeader>(ref header, NodeHeaderOffset);
        }

        /// <summary>
        /// Initialise the nodes of the circular linked-list. Only applicable if <see cref="SharedBuffer.IsOwnerOfSharedMemory"/> is true.
        /// </summary>
        private void InitialiseLinkedListNodes()
        {
            if (!IsOwnerOfSharedMemory)
                return;

            int N = 0;

            Node[] nodes = new Node[NodeCount];

            // First node
            nodes[N].Next = 1;
            nodes[N].Prev = NodeCount - 1;
            nodes[N].Offset = NodeBufferOffset;
            nodes[N].Index = N;
            // Middle nodes
            for (N = 1; N < NodeCount - 1; N++)
            {
                nodes[N].Next = N + 1;
                nodes[N].Prev = N - 1;
                nodes[N].Offset = NodeBufferOffset + (NodeBufferSize * N);
                nodes[N].Index = N;
            }
            // Last node
            nodes[N].Next = 0;
            nodes[N].Prev = NodeCount - 2;
            nodes[N].Offset = NodeBufferOffset + (NodeBufferSize * N);
            nodes[N].Index = N;

            // Write the nodes to the shared memory
            base.WriteArray<Node>(nodes, 0, nodes.Length, NodeOffset);
        }

        /// <summary>
        /// Closes the events. The shared memory could still be open within one or more other instances.
        /// </summary>
        protected override void DoClose()
        {
            if (DataExists != null)
            {
                (DataExists as IDisposable).Dispose();
                DataExists = null;
                (NodeAvailable as IDisposable).Dispose();
                NodeAvailable = null;
            }

            _nodeHeader = null;
        }

#endregion

#region Node Writing

        /// <summary>
        /// Attempts to reserve a node from the linked-list for writing with the specified timeout.
        /// </summary>
        /// <param name="timeout">The number of milliseconds to wait if a node is not immediately available for writing.</param>
        /// <returns>An unsafe pointer to the node if successful, otherwise null</returns>
        protected virtual Node* GetNodeForWriting(int timeout)
        {
            for (; ; )
            {
                int blockIndex = _nodeHeader->WriteStart;
                Node* node = this[blockIndex];
                if (node->Next == _nodeHeader->ReadEnd)
                {
                    // No room is available, wait for room to become available
                    var waits = Interlocked.Increment(ref _nodeHeader->WriteWaits);
                    //Console.WriteLine($"WaitW WS:{blockIndex} Waits:{waits}");
                    if (NodeAvailable.WaitOne(timeout))
                    {
                        //Console.WriteLine($"[{Runtime.RuntimeContext.Current.RuntimeId}]WaitWDone: {blockIndex} Waits: {_nodeHeader->WriteWaits}");
                        continue;
                    }

                    // Timeout
                    Log.Debug($"[{Name}] wait for room timeout");
                    return null;
                }

#pragma warning disable 0420 // ignore ref to volatile warning - Interlocked API
                if (Interlocked.CompareExchange(ref _nodeHeader->WriteStart, node->Next, blockIndex) == blockIndex)
                    return node;
#pragma warning restore 0420

                // Another thread has already acquired this node for writing, try again.
                continue;
            }
        }

        /// <summary>
        /// Makes a node available for reading after writing is complete
        /// </summary>
        /// <param name="node">An unsafe pointer to the node to return</param>
        protected virtual void PostNode(Node* node)
        {
            // Set the write flag for this node (the node is reserved so no need for locks)
            node->DoneWrite = 1;

            // Move the write pointer as far forward as we can
            // always starting from WriteEnd to make all contiguous
            // completed nodes available for reading.
            for (; ; )
            {
                int blockIndex = _nodeHeader->WriteEnd;
                node = this[blockIndex];
#pragma warning disable 0420 // ignore ref to volatile warning - Interlocked API
                if (Interlocked.CompareExchange(ref node->DoneWrite, 0, 1) != 1)
                {
                    // If we get here then another thread either another thread
                    // has already moved the write index or we have moved forward 
                    // as far as we can
                    return;
                }

                // Move the pointer one forward
                Interlocked.CompareExchange(ref _nodeHeader->WriteEnd, node->Next, blockIndex);
#pragma warning restore 0420

                // Signal the "data exists" event if read threads are waiting
                if (blockIndex == _nodeHeader->ReadStart)
                {
#if Linux
                    DataExists.Post();
#else
                    DataExists.Set();
#endif
                    //Console.WriteLine($"DataExists: {blockIndex}");

                }
            }
        }

        /// <summary>
        /// Writes the byte array buffer to the next available node for writing
        /// </summary>
        /// <param name="source">Reference to the buffer to write</param>
        /// <param name="startIndex">The index within the buffer to start writing from</param>
        /// <param name="timeout">The maximum number of milliseconds to wait for a node to become available for writing (default 1000ms)</param>
        /// <returns>The number of bytes written</returns>
        /// <remarks>The maximum number of bytes that can be written is the minimum of the length of <paramref name="source"/> and <see cref="NodeBufferSize"/>.</remarks>
        public virtual int Write(byte[] source, int startIndex = 0, int timeout = 1000)
        {
            // Grab a node for writing
            Node* node = GetNodeForWriting(timeout);
            if (node == null) return 0;

            // Copy the data
            int amount = Math.Min(source.Length - startIndex, NodeBufferSize);

            Marshal.Copy(source, startIndex, new IntPtr(BufferStartPtr + node->Offset), amount);
            node->AmountWritten = amount;


            // Writing is complete, make readable
            PostNode(node);

            return amount;
        }

        /// <summary>
        /// Writes the structure array buffer to the next available node for writing
        /// </summary>
        /// <param name="source">Reference to the buffer to write</param>
        /// <param name="startIndex">The index within the buffer to start writing from</param>
        /// <param name="timeout">The maximum number of milliseconds to wait for a node to become available for writing (default 1000ms)</param>
        /// <returns>The number of elements written</returns>
        /// <remarks>The maximum number of elements that can be written is the minimum of the length of <paramref name="source"/> subtracted by <paramref name="startIndex"/> and <see cref="NodeBufferSize"/> divided by <code>FastStructure.SizeOf&gt;T&lt;()</code>.</remarks>        
        //public virtual int Write<T>(T[] source, int startIndex = 0, int timeout = 1000)
        //    where T : struct
        //{
        //    // Grab a node for writing
        //    Node* node = GetNodeForWriting(timeout);
        //    if (node == null) return 0;

        //    // Write the data using the FastStructure class (much faster than the MemoryMappedViewAccessor WriteArray<T> method)
        //    int count = Math.Min(source.Length - startIndex, NodeBufferSize / FastStructure.SizeOf<T>());
        //    base.WriteArray<T>(source, startIndex, count, node->Offset);
        //    node->AmountWritten = count * FastStructure.SizeOf<T>();

        //    // Writing is complete, make node readable
        //    PostNode(node);

        //    return count;
        //}

        /// <summary>
        /// Writes the structure to the next available node for writing
        /// </summary>
        /// <typeparam name="T">The structure type to be written</typeparam>
        /// <param name="source">The structure to be written</param>
        /// <param name="timeout">The maximum number of milliseconds to wait for a node to become available for writing (default 1000ms)</param>
        /// <returns>The number of bytes written - larger than 0 if successful</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the size of the <typeparamref name="T"/> structure is larger than <see cref="NodeBufferSize"/>.</exception>
        public virtual int Write<T>(ref T source, int timeout = 1000)
            where T : struct
        {
            int structSize = Marshal.SizeOf(typeof(T));
            if (structSize > NodeBufferSize)
                throw new ArgumentOutOfRangeException("T", "The size of structure " + typeof(T).Name + " is larger than NodeBufferSize");

            // Attempt to retrieve a node for writing
            Node* node = GetNodeForWriting(timeout);
            if (node == null) return 0;

            // Copy the data using the MemoryMappedViewAccessor
            base.Write<T>(ref source, node->Offset);
            node->AmountWritten = structSize;

            // Return the node for further writing
            PostNode(node);

            return structSize;
        }

        /// <summary>
        /// Writes <paramref name="length"/> bytes from <paramref name="source"/> to the next available node for writing
        /// </summary>
        /// <param name="source">Pointer to the buffer to copy</param>
        /// <param name="timeout">The maximum number of milliseconds to wait for a node to become available (default 1000ms)</param>
        /// <param name="length">The number of bytes to attempt to write</param>
        /// <returns>The number of bytes written</returns>
        /// <remarks>The maximum number of bytes that can be written is the minimum of <paramref name="length"/> and <see cref="NodeBufferSize"/>.</remarks>        
        public virtual int Write(IntPtr source, int length, int timeout = 1000)
        {
            // Grab a node for writing
            Node* node = GetNodeForWriting(timeout);
            if (node == null) return 0;

            // Copy the data
            int amount = Math.Min(length, NodeBufferSize);
            base.Write(source, amount, node->Offset);
            node->AmountWritten = amount;

            // Writing is complete, make readable
            PostNode(node);

            return amount;
        }

        /// <summary>
        /// Reserves a node for writing and then calls the provided <paramref name="writeFunc"/> to perform the write operation.
        /// </summary>
        /// <param name="writeFunc">A function to used to write to the node's buffer. The first parameter is a pointer to the node's buffer. 
        /// The provided function should return the number of bytes written.</param>
        /// <param name="timeout">The maximum number of milliseconds to wait for a node to become available for writing (default 1000ms)</param>
        /// <returns>The number of bytes written</returns>
        public virtual int Write(Func<IntPtr, int> writeFunc, int timeout = 1000)
        {
            // Grab a node for writing
            Node* node = GetNodeForWriting(timeout);
            if (node == null) return 0;

            int amount = 0;
            try
            {
                // Pass destination IntPtr to custom write function
                amount = writeFunc(new IntPtr(BufferStartPtr + node->Offset));
                node->AmountWritten = amount;
            }
            finally
            {
                // Writing is complete, make readable
                PostNode(node);
            }

            return amount;
        }

#endregion

#region Node Reading

        /// <summary>
        /// Returns a copy of the shared memory header
        /// </summary>
        public NodeHeader ReadNodeHeader()
        {
            return (NodeHeader)Marshal.PtrToStructure(new IntPtr(_nodeHeader), typeof(NodeHeader));
        }

        /// <summary>
        /// Attempts to reserve a node from the linked-list for reading with the specified timeout
        /// </summary>
        /// <param name="timeout">The number of milliseconds to wait if a node is not immediately available for reading.</param>
        /// <returns>An unsafe pointer to the node if successful, otherwise null</returns>
        protected virtual Node* GetNodeForReading(int timeout)
        {
            for (; ; )
            {
                int blockIndex = _nodeHeader->ReadStart;
                Node* node = this[blockIndex];
                if (blockIndex == _nodeHeader->WriteEnd)
                {
                    // No data is available, wait for it
                    //Log.Debug($"[{Name}] No data is available, wait for {timeout}");
                    if (DataExists.WaitOne(timeout))
                    {
                        continue;
                    }

                    // Timeout
                    Log.Debug($"[{Name}] read timeout");
                    return null;
                }

#pragma warning disable 0420 // ignore ref to volatile warning - Interlocked API
                if (Interlocked.CompareExchange(ref _nodeHeader->ReadStart, node->Next, blockIndex) == blockIndex)
                    return node;
#pragma warning restore 0420

                // Another thread has already acquired this node for reading, try again
                continue;
            }
        }

        /// <summary>
        /// Returns a node to the available list of nodes for writing.
        /// </summary>
        /// <param name="node">An unsafe pointer to the node to be returned</param>
        protected virtual void ReturnNode(Node* node)
        {
            // Set the finished reading flag for this node (the node is reserved so no need for locks)
            node->DoneRead = 1;
            // Keep it clean and reset AmountWritten to prepare it for next Write
            node->AmountWritten = 0;

            // Move the read pointer forward as far as possible
            // always starting from ReadEnd to make all contiguous
            // read nodes available for writing.
            for (; ; )
            {
                int blockIndex = _nodeHeader->ReadEnd;
                node = this[blockIndex];
#pragma warning disable 0420 // ignore ref to volatile warning - Interlocked API
                if (Interlocked.CompareExchange(ref node->DoneRead, 0, 1) != 1)
                {
                    // If we get here then another read thread has already moved the pointer
                    // or we have moved ReadEnd as far forward as we can
                    return;
                }

                // Move the pointer forward one node
                Interlocked.CompareExchange(ref _nodeHeader->ReadEnd, node->Next, blockIndex);
#pragma warning restore 0420

                if (_nodeHeader->WriteWaits > 0)
                {
                    var waits = Interlocked.Decrement(ref _nodeHeader->WriteWaits);
                    if (waits < 0)
                    {
                        Interlocked.Increment(ref _nodeHeader->WriteWaits);
                    }
                    else
                    {
                        //TODO:考虑释放多个后续已归还的
#if Linux
                        NodeAvailable.Post();
#else
                        NodeAvailable.Set();
#endif
                    }
                }
                //if (node->Prev == _nodeHeader->WriteStart)
                //    NodeAvailable.Set();
            }
        }

        /// <summary>
        /// Reads the next available node for reading into the specified byte array
        /// </summary>
        /// <param name="destination">Reference to the buffer</param>
        /// <param name="startIndex">The index within the buffer to start writing from</param>
        /// <param name="timeout">The maximum number of milliseconds to wait for a node to become available for reading (default 1000ms)</param>
        /// <returns>The number of bytes read</returns>
        /// <remarks>The maximum number of bytes that can be read is the minimum of the length of <paramref name="destination"/> subtracted by <paramref name="startIndex"/> and <see cref="NodeBufferSize"/>.</remarks>
        public virtual int Read(byte[] destination, int startIndex = 0, int timeout = 1000)
        {
            Node* node = GetNodeForReading(timeout);
            if (node == null) return 0;

            //int amount = Math.Min(buffer.Length, NodeBufferSize);
            int amount = Math.Min(destination.Length - startIndex, node->AmountWritten);

            // Copy the data
            Marshal.Copy(new IntPtr(BufferStartPtr + node->Offset), destination, startIndex, amount);

            // Return the node for further writing
            ReturnNode(node);

            return amount;
        }

        /// <summary>
        /// Reads the next available node for reading into the specified structure array
        /// </summary>
        /// <typeparam name="T">The structure type to be read</typeparam>
        /// <param name="destination">Reference to the buffer</param>
        /// <param name="startIndex">The index within the destination to start writing to.</param>
        /// <param name="timeout">The maximum number of milliseconds to wait for a node to become available for reading (default 1000ms)</param>
        /// <returns>The number of elements read into destination</returns>
        /// <remarks>The maximum number of elements that can be read is the minimum of the length of <paramref name="destination"/> subtracted by <paramref name="startIndex"/> and <see cref="Node.AmountWritten"/> divided by <code>FastStructure.SizeOf&gt;T&lt;()</code>.</remarks>
        //public virtual int Read<T>(T[] destination, int startIndex = 0, int timeout = 1000)
        //    where T : struct
        //{
        //    Node* node = GetNodeForReading(timeout);
        //    if (node == null) return 0;

        //    // Copy the data using the FastStructure class (much faster than the MemoryMappedViewAccessor ReadArray<T> method)
        //    int count = Math.Min(destination.Length - startIndex, node->AmountWritten / FastStructure.SizeOf<T>());
        //    base.ReadArray<T>(destination, startIndex, count, node->Offset);

        //    // Return the node for further writing
        //    ReturnNode(node);

        //    return count;
        //}

        /// <summary>
        /// Reads the next available node for reading into the a structure
        /// </summary>
        /// <typeparam name="T">The structure type to be read</typeparam>
        /// <param name="destination">The resulting structure if successful otherwise default(T)</param>
        /// <param name="timeout">The maximum number of milliseconds to wait for a node to become available for reading (default 1000ms)</param>
        /// <returns>The number of bytes read</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the size of <typeparamref name="T"/> is larger than <see cref="NodeBufferSize"/>.</exception>
        public virtual int Read<T>(out T destination, int timeout = 1000)
            where T : struct
        {
            int structSize = Marshal.SizeOf(typeof(T));
            if (structSize > NodeBufferSize)
                throw new ArgumentOutOfRangeException("T", "The size of structure " + typeof(T).Name + " is larger than NodeBufferSize");

            // Attempt to retrieve a node
            Node* node = GetNodeForReading(timeout);
            if (node == null)
            {
                destination = default(T);
                return 0;
            }

            // Copy the data using the MemoryMappedViewAccessor
            base.Read<T>(out destination, node->Offset);

            // Return the node for further writing
            ReturnNode(node);

            return structSize;
        }

        /// <summary>
        /// Reads the next available node for reading into the specified memory location with the specified length
        /// </summary>
        /// <param name="destination">Pointer to the buffer</param>
        /// <param name="length">The maximum length of <paramref name="destination"/></param>
        /// <param name="timeout">The maximum number of milliseconds to wait for a node to become available for reading (default 1000ms)</param>
        /// <returns>The number of bytes read</returns>
        /// <remarks>The maximum number of bytes that can be read is the minimum of the <paramref name="length"/> and <see cref="Node.AmountWritten"/>.</remarks>
        public virtual int Read(IntPtr destination, int length, int timeout = 1000)
        {
            Node* node = GetNodeForReading(timeout);
            if (node == null) return 0;

            //int amount = Math.Min(length, NodeBufferSize);
            int amount = Math.Min(length, node->AmountWritten);

            // Copy the data
            base.Read(destination, amount, node->Offset);

            // Return node for further writing
            ReturnNode(node);

            return amount;
        }

        /// <summary>
        /// Reserves a node for reading and then calls the provided <paramref name="readFunc"/> to perform the read operation.
        /// </summary>
        /// <param name="readFunc">A function used to read from the node's buffer. The first parameter is a pointer to the node's buffer. 
        /// The provided function should return the number of bytes read.</param>
        /// <param name="timeout">The maximum number of milliseconds to wait for a node to become available for reading (default 1000ms)</param>
        /// <returns>The number of bytes read</returns>
        public virtual int Read(Func<IntPtr, int> readFunc, int timeout = 1000)
        {
            Node* node = GetNodeForReading(timeout);
            if (node == null) return 0;

            int amount = 0;
            try
            {
                // Pass pointer to buffer directly to custom read function
                amount = readFunc(new IntPtr(BufferStartPtr + node->Offset));
            }
            finally
            {
                // Return the node for further writing
                ReturnNode(node);
            }
            return amount;
        }

#endregion
    }
}
