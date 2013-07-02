﻿// Copyright (C) Pash Contributors. License: GPL/BSD. See https://github.com/Pash-Project/Pash/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;

namespace Microsoft.PowerShell.Commands.Utility
{
    /// <summary>
    ///     Helper class for easily implementing -Unique on cmdlets like
    ///     Sort-Object or Select-Object.
    /// </summary>
    internal class PropertyEqualityComparer : EqualityComparer<PSObject>
    {
        public List<string> Properties { get; set; }

        public PropertyEqualityComparer(List<string> properties)
        {
            this.Properties = properties;
        }

        public override bool Equals(PSObject x, PSObject y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            if (Properties == null)
                return x.BaseObject.Equals(y.BaseObject);

            foreach (var property in this.Properties)
            {
                var xPropertyValue = x.BaseObject.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(x.BaseObject, null);
                var yPropertyValue = y.BaseObject.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(y.BaseObject, null);

                if (!xPropertyValue.Equals(yPropertyValue))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode(PSObject obj)
        {
            if (Properties == null)
            {
                return obj.GetHashCode();
            }

            int hashCode = 0;

            foreach (var property in this.Properties)
            {
                var propertyValue = obj.BaseObject.GetType().GetProperty(property.ToString(), BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(obj.BaseObject, null);
                hashCode ^= propertyValue.GetHashCode();
            }

            return hashCode;
        }
    }
}
