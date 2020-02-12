using System;
using System.Collections.Generic;

namespace OmniSharp.Models
{
    public static class SymbolKinds
    {
        // types
        public static readonly string Class = nameof(Class).ToLowerInvariant();
        public static readonly string Delegate = nameof(Delegate).ToLowerInvariant();
        public static readonly string Enum = nameof(Enum).ToLowerInvariant();
        public static readonly string Interface = nameof(Interface).ToLowerInvariant();
        public static readonly string Struct = nameof(Struct).ToLowerInvariant();

        // members
        public static readonly string Constant = nameof(Constant).ToLowerInvariant();
        public static readonly string Constructor = nameof(Constructor).ToLowerInvariant();
        public static readonly string Destructor = nameof(Destructor).ToLowerInvariant();
        public static readonly string EnumMember = nameof(EnumMember).ToLowerInvariant();
        public static readonly string Event = nameof(Event).ToLowerInvariant();
        public static readonly string Field = nameof(Field).ToLowerInvariant();
        public static readonly string Indexer = nameof(Indexer).ToLowerInvariant();
        public static readonly string Method = nameof(Method).ToLowerInvariant();
        public static readonly string Operator = nameof(Operator).ToLowerInvariant();
        public static readonly string Property = nameof(Property).ToLowerInvariant();

        // other
        public static readonly string Namespace = nameof(Namespace).ToLowerInvariant();
        public static readonly string Unknown = nameof(Unknown).ToLowerInvariant();

        enum MonacoSymbolKind
        {
            File = 0,
            Module = 1,
            Namespace = 2,
            Package = 3,
            Class = 4,
            Method = 5,
            Property = 6,
            Field = 7,
            Constructor = 8,
            Enum = 9,
            Interface = 10,
            Function = 11,
            Variable = 12,
            Constant = 13,
            String = 14,
            Number = 15,
            Boolean = 16,
            Array = 17,
            Object = 18,
            Key = 19,
            Null = 20,
            EnumMember = 21,
            Struct = 22,
            Event = 23,
            Operator = 24,
            TypeParameter = 25
        }

        private static readonly Dictionary<string, MonacoSymbolKind> kindMap = new Dictionary<string, MonacoSymbolKind>
        {
            { Class, MonacoSymbolKind.Class },
            { Delegate, MonacoSymbolKind.Class },
            { Enum, MonacoSymbolKind.Enum },
            { Interface, MonacoSymbolKind.Interface },
            { Struct, MonacoSymbolKind.Struct },

            { Constant, MonacoSymbolKind.Constant },
            { Destructor, MonacoSymbolKind.Method },
            { EnumMember, MonacoSymbolKind.EnumMember },
            { Event, MonacoSymbolKind.Event },
            { Field, MonacoSymbolKind.Field },
            { Indexer, MonacoSymbolKind.Property },
            { Method, MonacoSymbolKind.Method },
            { Operator, MonacoSymbolKind.Operator },
            { Property, MonacoSymbolKind.Property },

            { Namespace, MonacoSymbolKind.Namespace },
            { Unknown, MonacoSymbolKind.Class }
        };

        internal static int ToMonacoKind(string kind)
        {
            return (int)kindMap[kind];
        }
    }
}
