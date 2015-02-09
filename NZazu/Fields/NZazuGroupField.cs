using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace NZazu.Fields
{
    public class NZazuGroupField : NZazuField, INZazuWpfGroupField
    {
        public NZazuGroupField(string key) : base(key)
        {
        }

        public override string StringValue { get; set; }

        public override DependencyProperty ContentProperty { get { return null; } }
        public override string Type { get { return "group"; } }

        protected override Control GetValue()
        {
            return new ContentControl();
        }

        public IEnumerable<INZazuWpfField> Fields { get; private set; }
    }
}