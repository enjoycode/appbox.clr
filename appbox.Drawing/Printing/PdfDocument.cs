using System;
using System.IO;

namespace appbox.Drawing
{
    public sealed class PdfDocument : IDisposable
    {

        private IntPtr handle = IntPtr.Zero;
        // private SKManagedWStream stream = null;

        public PdfDocument(Stream outputStream)
        {
            // if (!outputStream.CanWrite)
            //     throw new InvalidOperationException("outputStream can not write.");

            // stream = new SKManagedWStream(outputStream, false);
            // this.handle = SkiaApi.sk_document_create_pdf_from_stream(stream.Handle, 72f); //注意：经测试dpi必须为72f
            // if (this.handle == IntPtr.Zero)
            //     throw new InvalidOperationException("Create pdf document handle failed.");
        }

        /// <summary>
        /// Begins the page.
        /// </summary>
        /// <returns>The handle to SkCanvas.</returns>
        /// <param name="width">页宽，单位：像素，转换dpi=72</param>
        /// <param name="height">页高，单位：像素，转换dpi=72</param>
        public IntPtr BeginPage(float width, float height)
        {
            throw new NotImplementedException();
            // SKRect contentRect = new SKRect(0, 0, width, height);
            // return SkiaApi.sk_document_begin_page(this.handle, width, height, ref contentRect);
        }

        public void EndPage()
        {
            throw new NotImplementedException();
            //SkiaApi.sk_document_end_page(this.handle);
        }

        #region ====IDisposable Support====
        private bool isDisposed = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                //     //free unmanaged resources (unmanaged objects) and override a finalizer below.
                //     //set large fields to null.
                //     if (this.handle != IntPtr.Zero)
                //     {
                //         //todo: how to release handle
                //         SkiaApi.sk_document_close(this.handle);
                //         this.handle = IntPtr.Zero;
                //     }

                //     if (disposing)
                //     {
                //         //dispose managed state (managed objects).
                //         if (stream != null)
                //         {
                //             stream.Dispose();
                //             stream = null;
                //         }
                //     }

                isDisposed = true;
            }
        }

        ~PdfDocument()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

