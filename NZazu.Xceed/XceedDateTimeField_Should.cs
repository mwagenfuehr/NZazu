using System;
using System.Threading;
using FluentAssertions;
using NEdifis.Attributes;
using NUnit.Framework;
using NZazu.Contracts;
using Xceed.Wpf.Toolkit;

namespace NZazu.Xceed
{
    [TestFixtureFor(typeof(XceedDateTimeField))]
    [Apartment(ApartmentState.STA)]
    // ReSharper disable InconsistentNaming
    internal class XceedDateTimeField_Should
    {
        [Test]
        public void Be_Creatable()
        {
            var sut = new XceedDateTimeField(new FieldDefinition {Key="test"});

            sut.Should().NotBeNull();
            sut.Should().BeAssignableTo<INZazuWpfField>();
            sut.Type.Should().Be("date");
        }

        [Test]
        public void Override_ContentProperty()
        {
            var sut = new XceedDateTimeField(new FieldDefinition { Key = "date" });
            sut.ContentProperty.Should().Be(DateTimePickerWithUpdate.ValueProperty);
        }

        [Test]
        [STAThread]
        public void Use_Format_Settings()
        {
            var sut = new XceedDateTimeField(new FieldDefinition { Key = "date" });
            const string dateFormat = "yyyy/MM/dd";
            sut.Settings.Add("Format", dateFormat);

            var control = (DateTimePickerWithUpdate)sut.ValueControl;
            control.Format.Should().Be(DateTimeFormat.Custom);
            control.FormatString.Should().Be(dateFormat);

            sut = new XceedDateTimeField(new FieldDefinition { Key = "date" });
            control = (DateTimePickerWithUpdate)sut.ValueControl;
            control.Format.Should().Be(DateTimeFormat.FullDateTime);
            control.FormatString.Should().BeNull();
        }
    }
}