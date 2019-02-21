﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NZazu.Contracts.Checks;

namespace NZazu.Contracts.FormChecks
{
    public class GreaterThanFormCheck : IFormCheck
    {
        internal class GreaterThanFormCheckSettings
        {
            public string Hint { get; set; }
            public string LeftFieldName { get; set; }
            public string RightFieldName { get; set; }
        }

        private GreaterThanFormCheckSettings Settings { get; }

        public GreaterThanFormCheck(IDictionary<string, string> settings)
        {
            Settings = settings.ToDictionary(x => x.Key, x => (object)x.Value).ToObject<GreaterThanFormCheckSettings>();
        }


        public ValueCheckResult Validate(FormData formData, IFormatProvider formatProvider = null)
        {
            var leftFieldValue = formData.Values?[Settings.LeftFieldName];
            var rightFieldValue = formData.Values?[Settings.RightFieldName];

            long.TryParse(leftFieldValue, out var leftNumber);
            long.TryParse(rightFieldValue, out var rightNumber);

            return leftNumber < rightNumber ? 
                new ValueCheckResult(false, new ArgumentException(Settings.Hint)) 
                : ValueCheckResult.Success;
        }
    }
}