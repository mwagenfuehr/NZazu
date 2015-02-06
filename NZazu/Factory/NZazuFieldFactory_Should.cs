using System;
using System.Collections.Generic;
using System.Windows.Controls;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using NZazu.Contracts;
using NZazu.Contracts.Checks;
using NZazu.Fields;

namespace NZazu.Factory
{
    [TestFixture, RequiresSTA]
    // ReSharper disable InconsistentNaming
    class NZazuFieldFactory_Should
    {
        [Test]
        public void Be_Creatable()
        {
            var sut = new NZazuFieldFactory();

            sut.Should().NotBeNull();
            sut.Should().BeAssignableTo<INZazuFieldFactory>();
        }

        [Test]
        [TestCase(null, typeof(Label))]
        [TestCase("label", typeof(Label))]
        [TestCase("string", typeof(TextBox))]
        [TestCase("bool", typeof(CheckBox))]
        [TestCase("int", typeof(TextBox))]
        [TestCase("date", typeof(DatePicker))]
        [TestCase("double", typeof(TextBox))]
        //[TestCase("dateTime", typeof (DatePicker))]
        public void Support(string fieldType, Type controlType)
        {
            var sut = new NZazuFieldFactory();

            var field = sut.CreateField(new FieldDefinition { Key = "test", Type = fieldType, Description = "test" });

            field.Should().NotBeNull();
            field.Type.Should().Be(fieldType ?? "label"); // because of the fallback in case of null

            var control = field.ValueControl;
            control.Should().BeOfType(controlType);
        }

        [Test]
        public void Return_Label_If_Not_Supported()
        {
            var sut = new NZazuFieldFactory();

            var field = sut.CreateField(new FieldDefinition { Key = "test", Type = "I am a not supported label", Description = "test" });

            field.Should().NotBeNull();
            field.Type.Should().Be("label", because: "the fallback is label"); // because of the fallback in case of null

            var control = field.ValueControl;
            control.Should().BeOfType<Label>();
        }

        [Test]
        public void Set_Checks()
        {
            var sut = new NZazuFieldFactory();
            var check = Substitute.For<IValueCheck>();
            var checks = new[] { check };
            var field = (NZazuField)sut.CreateField(new FieldDefinition { Key = "test", Type = "string", Checks = checks });

            field.Should().NotBeNull();
            field.Checks.Should().BeSameAs(checks);
        }

        [Test]
        public void Set_Settings()
        {
            var sut = new NZazuFieldFactory();
            var settings = new Dictionary<string, string>() { { "Format", "#.##" }, { "Color", "Green" } };
            var field = (NZazuField)sut.CreateField(new FieldDefinition { Key = "test", Type = "string", Settings = settings });

            field.Should().NotBeNull();
            field.Settings.Should().BeSameAs(settings);
        }
    }
}