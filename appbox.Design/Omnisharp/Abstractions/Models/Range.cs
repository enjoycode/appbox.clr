﻿using System;
using System.Collections.Generic;

namespace OmniSharp.Models
{
    public class Range : IEquatable<Range>
    {
        public Point Start { get; set; }
        public Point End { get; set; }

        public bool Contains(int line, int column)
        {
            if (Start.Line > line || End.Line < line)
            {
                return false;
            }

            if (Start.Line == line && Start.Column > column)
            {
                return false;
            }

            if (End.Line == line && End.Column < column)
            {
                return false;
            }

            return true;
        }

        public override bool Equals(object obj)
            => Equals(obj as Range);

        public bool Equals(Range other)
            => other != null
                && EqualityComparer<Point>.Default.Equals(Start, other.Start)
                && EqualityComparer<Point>.Default.Equals(End, other.End);

        public override int GetHashCode()
        {
            var hashCode = -1676728671;
            hashCode = hashCode * -1521134295 + EqualityComparer<Point>.Default.GetHashCode(Start);
            hashCode = hashCode * -1521134295 + EqualityComparer<Point>.Default.GetHashCode(End);
            return hashCode;
        }

        public override string ToString()
            => $"Start = {{{Start}}}, End = {{{End}}}";

        public static bool operator ==(Range range1, Range range2)
            => EqualityComparer<Range>.Default.Equals(range1, range2);

        public static bool operator !=(Range range1, Range range2)
            => !(range1 == range2);

        internal void WriteToJson(System.Text.Json.Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteNumber("startLineNumber", Start.Line + 1); //注意往前传+1，非-1
            writer.WriteNumber("startColumn", Start.Column + 1);
            writer.WriteNumber("endLineNumber", End.Line + 1);
            writer.WriteNumber("endColumn", End.Column + 1);
            writer.WriteEndObject();
        }
    }
}
