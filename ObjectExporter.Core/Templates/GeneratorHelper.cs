﻿using System;
using System.Collections.Generic;
using EnvDTE;
using EnvDTE80;
using ObjectExporter.Core.ExtensionMethods;
using ObjectExporter.Core.Globals;

namespace ObjectExporter.Core.Templates
{
    public static class GeneratorHelper
    {
        public static bool CanBeExpressedAsSingleType(string expressionType)
        {
            switch (expressionType)
            {
                case "System.Guid":
                case "System.TimeSpan":
                case "System.DateTimeOffset":
                case "System.Char":
                case "char":
                case "System.DateTime":
                case "System.Decimal":
                case "decimal":
                case "System.Single":
                case "float":
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsBase(Expression expression)
        {
            return (expression.Name == "base" && expression.Type.Contains("{"));
        }

        private static List<string> SimpleTypes = new List<string>()
        {
            "bool",
            "byte",
            "sbyte",
            "char",
            "decimal",
            "double",
            "float",
            "int",
            "uint",
            "long",
            "ulong",
            "object",
            "short",
            "ushort",
            "string"
        };

        public static string WriteCommaIfNotLast(bool isLast)
        {
            if (isLast) return "";
            else return ",";
        }

        public static bool IsSerializable(string expressionName)
        {
            switch (expressionName)
            {
                case "Raw View":
                case "Static members":
                case "Non-Public members":
                    return false;
                default:
                    return true;
            }
        }

        public static bool IsTypeOfCollection(string expressionType)
        {
            return (expressionType.Contains("<") || expressionType.Contains(">") || expressionType.Contains("[") ||
                expressionType.Contains("]") || expressionType.Contains("Count ="));
        }

        public static bool IsCollectionMember(string expressionName)
        {
            return (expressionName.Contains("[") || expressionName.Contains("]"));
        }

        public static string StripCurleyBraces(string input)
        {
            return input.Replace("{", "").Replace("}", "");
        }

        public static string StripObjectReference(string input)
        {
            if (input.Contains("{") && input.Contains("}"))
            {
                return input.Between('{', '}').Trim();
            }
            else
            {
                return input;
            }
        }

        public static string GetBugFixedDateTimeOffset(Expression expression)
        {
            string dateTimePartStr = StripCurleyBraces(expression.DataMembers.Item(2).Value);
            string offsetPartStr = StripCurleyBraces(expression.DataMembers.Item(11).Value);

            TimeSpan offset = TimeSpan.Parse(offsetPartStr);
            DateTime dateTime = DateTime.Parse(dateTimePartStr);

            DateTimeOffset dateTimeOffset = new DateTimeOffset(dateTime, offset);

            return dateTimeOffset.ToString();
        }

        public static string ResolveReservedNames(string expressionName)
        {
            if (ReservedWords.CSharp.Contains(expressionName))
            {
                return ("@" + expressionName);
            }
            else
            {
                return expressionName;
            }
        }
    }
}
