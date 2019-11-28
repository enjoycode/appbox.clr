using System;
using System.Globalization;
using System.Linq;
using appbox.Models;

namespace appbox.Design
{
    /// <summary>
    /// 代码帮助类，主要用于检验变量名称等是否合法
    /// </summary>
    static class CodeHelper
    {

        private static readonly string[] _keywords = { "private", "protected" };

        public static bool IsValidIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            if (value.Length > 0x200)
            {
                return false;
            }
            if (value[0] != '@')
            {
                if (IsKeyword(value))
                {
                    return false;
                }
            }
            else
            {
                value = value.Substring(1);
            }
            return IsValidLanguageIndependentIdentifier(value);
        }

        private static bool IsKeyword(string value)
        {
            return _keywords.Contains(value);
        }

        public static bool IsValidLanguageIndependentIdentifier(string value)
        {
            return IsValidTypeNameOrIdentifier(value, false);
        }

        private static bool IsValidTypeNameOrIdentifier(string value, bool isTypeName)
        {
            bool nextMustBeStartChar = true;
            if (value.Length == 0)
            {
                return false;
            }
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (char.GetUnicodeCategory(c))
                {
                    case UnicodeCategory.UppercaseLetter:
                    case UnicodeCategory.LowercaseLetter:
                    case UnicodeCategory.TitlecaseLetter:
                    case UnicodeCategory.ModifierLetter:
                    case UnicodeCategory.OtherLetter:
                    case UnicodeCategory.LetterNumber:
                        {
                            nextMustBeStartChar = false;
                            continue;
                        }
                    case UnicodeCategory.NonSpacingMark:
                    case UnicodeCategory.SpacingCombiningMark:
                    case UnicodeCategory.DecimalDigitNumber:
                    case UnicodeCategory.ConnectorPunctuation:
                        if (!nextMustBeStartChar || (c == '_'))
                        {
                            break;
                        }
                        return false;

                    default:
                        if (!isTypeName || !IsSpecialTypeChar(c, ref nextMustBeStartChar))
                        {
                            return false;
                        }
                        break;
                }
                nextMustBeStartChar = false;
                continue;
            }
            return true;
        }

        private static bool IsSpecialTypeChar(char ch, ref bool nextMustBeStartChar)
        {
            switch (ch)
            {
                case '[':
                case ']':
                case '$':
                case '&':
                case '*':
                case '+':
                case ',':
                case '-':
                case '.':
                case ':':
                case '<':
                case '>':
                    nextMustBeStartChar = true;
                    return true;

                case '`':
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 获取模型类型的复数名称
        /// </summary>
        /// <param name="modelType"></param>
        /// <returns></returns>
        public static string GetPluralStringOfModelType(ModelType modelType)
        {
            switch (modelType)
            {
                case ModelType.Enum:
                    return "Enums";
                case ModelType.Entity:
                    return "Entities";
                case ModelType.Event:
                    return "Events";
                case ModelType.Service:
                    return "Services";
                case ModelType.View:
                    return "Views";
                case ModelType.Workflow:
                    return "Workflows";
                case ModelType.Report:
                    return "Reports";
                case ModelType.Permission:
                    return "Permissions";
                case ModelType.Application:
                    return "Applications";
                default:
                    throw new ArgumentException();
            }
        }

        public static ModelType GetModelTypeFromPluralString(string type)
        {
            switch (type)
            {
                case "Entities":
                    return ModelType.Entity;
                case "Services":
                    return ModelType.Service;
                case "Views":
                    return ModelType.View;
                case "Permissions":
                    return ModelType.Permission;
                case "Enums":
                    return ModelType.Enum;
                case "Events":
                    return ModelType.Event;
                //case "MenuItems":
                    //return ModelType.MenuItem;
                default:
                    throw ExceptionHelper.NotImplemented();
            }
        }

    }

}
