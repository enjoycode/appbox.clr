namespace appbox.Reporting.RDL
{
    ///<summary>
    ///  Handles the Image source enumeration.  External, Embedded, Database
    ///</summary>
    internal enum ImageSourceEnum
    {
        /// <summary>
        /// The Value contains a constant or
        /// expression that evaluates to the location of the image
        /// </summary>
        External,
        /// <summary>
        /// The Value contains a constant
        /// or expression that evaluates to the name of
        /// an EmbeddedImage within the report.
        /// </summary>
        Embedded,
        /// <summary>
        /// The Value contains an
        /// expression (typically a field in the database)
        /// that evaluates to the binary data for the
        /// image.
        /// </summary>
        Database,
        /// <summary>
        /// Illegal or unspecified
        /// </summary>
        Unknown
    }

    internal class ImageSource
    {
        static internal ImageSourceEnum GetStyle(string s)
        {
            var rs = s switch
            {
                "External" => ImageSourceEnum.External,
                "Embedded" => ImageSourceEnum.Embedded,
                "Database" => ImageSourceEnum.Database,
                _ => ImageSourceEnum.Unknown,
            };
            return rs;
        }
    }

}
