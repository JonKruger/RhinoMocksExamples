using NBehave.Spec.NUnit;
using NUnit.Framework;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using System.Collections.Generic;
using System;
using Is = Rhino.Mocks.Constraints.Is;
using Rhino.Mocks.Exceptions;

namespace RhinoMocksExamples
{
    // Here are some tests that will show what how stubs, mocks, and
    // partial mocks work in Rhino Mocks.

    public class When_working_with_a_stub_of_an_interface : SpecBase
    {
        public interface ISampleClass
        {
            string Property { get; set; }
            void VoidMethod();
            int MethodThatReturnsInteger(string s);
            object MethodThatReturnsObject(int i);
            void MethodWithOutParameter(out int i);
            void MethodWithRefParameter(ref string i);

            event EventHandler SomeEvent;
        }

        [Test]
        public void You_can_create_a_stub_by_calling_MockRepository_GenerateStub()
        {
            // This is the out-of-the-box way to create a stub in Rhino Mocks.
            var stub = MockRepository.GenerateStub<ISampleClass>();
        }

        [Test]
        public void NBehave_gives_us_a_shorthand_way_of_creating_stubs()
        {
            var stub = CreateStub<ISampleClass>();
        }

        [Test]
        public void Calling_void_methods_will_do_nothing()
        {
            var stub = CreateStub<ISampleClass>();
            stub.VoidMethod();
        }

        [Test]
        public void Calling_methods_that_return_value_types_will_return_the_default_value_for_that_type()
        {
            var stub = CreateStub<ISampleClass>();
            stub.MethodThatReturnsInteger("foo").ShouldEqual(0);
        }

        [Test]
        public void Calling_methods_that_return_reference_types_will_return_null()
        {
            var stub = CreateStub<ISampleClass>(); 
            stub.MethodThatReturnsObject(1).ShouldBeNull();
        }

        [Test]  
        public void Handling_events_will_do_nothing()
        {
            var stub = CreateStub<ISampleClass>();
            stub.SomeEvent += (args, e) => { };
        }

        [Test]
        public void You_can_tell_the_stub_what_value_to_return_when_is_method_is_called_with_specific_arguments()
        {
            var stub = CreateStub<ISampleClass>();
            
            stub.Stub(s => s.MethodThatReturnsInteger("foo")).Return(5);

            // calling the method with "foo" as the parameter will return 5
            stub.MethodThatReturnsInteger("foo").ShouldEqual(5);

            // calling the method with anything other than "foo" as the 
            // parameter will return the default value
            stub.MethodThatReturnsInteger("bar").ShouldEqual(0);
        }

        [Test]
        public void You_can_tell_the_stub_what_value_to_return_when_is_method_is_called_with_any_argument()
        {
            var stub = CreateStub<ISampleClass>();

            stub.Stub(s => s.MethodThatReturnsInteger(Arg<string>.Is.Anything)).Return(5);

            // now it doesn't matter what the parameter is, we'll always get 5
            stub.MethodThatReturnsInteger("foo").ShouldEqual(5);
            stub.MethodThatReturnsInteger("bar").ShouldEqual(5);
            stub.MethodThatReturnsInteger(null).ShouldEqual(5);
        }

        [Test]
        public void You_can_get_fancy_with_parameters_in_stubs()
        {
            var stub = CreateStub<ISampleClass>();

            // Arg<>.Matches() allows us to specify a lambda expression that specifies
            // whether the return value should be used in this case.  Here we're saying
            // that we'll return 5 if the string passed in is longer than 2 characters.
            stub.Stub(s => s.MethodThatReturnsInteger(Arg<string>.Matches(arg => arg != null && arg.Length > 2)))
                .Return(5);

            stub.MethodThatReturnsInteger("fooo").ShouldEqual(5);
            stub.MethodThatReturnsInteger("foo").ShouldEqual(5);
            stub.MethodThatReturnsInteger("fo").ShouldEqual(0);
            stub.MethodThatReturnsInteger("f").ShouldEqual(0);
            stub.MethodThatReturnsInteger(null).ShouldEqual(0);
        }

        [Test]
        public void Handling_out_parameters_in_stubs()
        {
            var stub = CreateStub<ISampleClass>();

            // Here's how you stub an "out" parameter.  The "Dummy" part is 
            // just to satisfy the compiler.
            stub.Stub(s => s.MethodWithOutParameter(out Arg<int>.Out(10).Dummy));

            int i = 12345;
            stub.MethodWithOutParameter(out i);
            i.ShouldEqual(10);
        }

        [Test]
        public void Handling_ref_parameters_in_stubs()
        {
            var stub = CreateStub<ISampleClass>();

            // Here's how you stub an "ref" parameter.  The "Dummy" part is 
            // just to satisfy the compiler.  (Note: Is.Equal() is part of
            // the Rhino.Mocks.Contraints namespace, there is also an 
            // Is.EqualTo() in NUnit... this is not what you want here.)
            stub.Stub(s => s.MethodWithRefParameter(ref Arg<string>.Ref(Is.Equal("input"), "output").Dummy));

            // If you call the method with the specified input argument, it will
            // change the parameter to the value you specified.
            string param = "input";
            stub.MethodWithRefParameter(ref param);
            param.ShouldEqual("output");

            // If I call the method with any other input argument, it won't
            // change the value.
            param = "some other value";
            stub.MethodWithRefParameter(ref param);
            param.ShouldEqual("some other value");
        }

        [Test]
        public void You_can_tell_the_stub_to_throw_an_exception_when_a_method_is_called()
        {
            var stub = CreateStub<ISampleClass>();
            
            // calling the method with "foo" as the parameter will throw exception
            stub.Stub(s => s.MethodThatReturnsInteger("foo")).Throw(new InvalidOperationException());

            typeof(InvalidOperationException).ShouldBeThrownBy(
                () => stub.MethodThatReturnsInteger("foo"));
        }

        [Test]
        public void You_can_check_to_see_if_a_method_was_called()
        {
            var stub = CreateStub<ISampleClass>();
            
            stub.MethodThatReturnsInteger("foo");

            stub.AssertWasCalled(s => s.MethodThatReturnsInteger("foo"));
            stub.AssertWasCalled(s => s.MethodThatReturnsInteger(Arg<string>.Is.Anything));
        }

        [Test]
        public void You_can_check_to_see_if_a_method_was_called_a_certain_number_of_times()
        {
            var stub = CreateStub<ISampleClass>();

            stub.MethodThatReturnsInteger("foo");
            stub.MethodThatReturnsInteger("bar");

            // this will pass
            stub.AssertWasCalled(s => s.MethodThatReturnsInteger("foo"), o => o.Repeat.Once());

            // call the method a second time
            stub.MethodThatReturnsInteger("foo");

            // now this will fail because we called it a second time
            typeof (ExpectationViolationException).ShouldBeThrownBy(
                () => stub.AssertWasCalled(s => s.MethodThatReturnsInteger("foo"), o => o.Repeat.Once()));
        }

        [Test]
        public void Another_way_to_verify_expectations_instead_of_AssertWasCalled()
        {
            var stub = CreateStub<ISampleClass>();

            // Here I'm setting up an expectation that a method will be called
            stub.Expect(s => s.MethodThatReturnsInteger("foo")).Return(5);

            var output = stub.MethodThatReturnsInteger("foo");
            output.ShouldEqual(5);

            // ... and now I'm verifying that the method was called
            stub.VerifyAllExpectations();
        }

        [Test]
        public void You_can_check_to_see_if_a_method_was_not_called()
        {
            var stub = CreateStub<ISampleClass>();

            stub.MethodThatReturnsInteger("foo");

            stub.AssertWasNotCalled(s => s.MethodThatReturnsInteger("asdfdsf"));
            stub.AssertWasNotCalled(s => s.MethodThatReturnsObject(Arg<int>.Is.Anything));
            stub.AssertWasNotCalled(s => s.VoidMethod());
        }

        [Test]
        public void You_can_get_the_arguments_of_calls_to_a_method()
        {
            var stub = CreateStub<ISampleClass>();

            stub.MethodThatReturnsInteger("foo");
            stub.MethodThatReturnsInteger("bar");

            // GetArgumentsForCallsMadeOn() returns a list of arrays that contain
            // the parameter values for each call to the method.
            IList<object[]> argsPerCall = stub.GetArgumentsForCallsMadeOn(s => s.MethodThatReturnsInteger(null));
            argsPerCall[0][0].ShouldEqual("foo");
            argsPerCall[1][0].ShouldEqual("bar");
        }

        //[Test]
        //public void If_you_set_a_property_the_getter_will_return_the_value()
        //{
        //    _stub.Property = "foo";
        //    _stub.Property.ShouldEqual("foo");
        //}
    }

    //public class When_working_with_a_mock_of_an_interface : SpecBase
    //{
    //    private ISampleClass _stub;

    //    public interface ISampleClass
    //    {
    //        string Property { get; set; }
    //        void VoidMethod();
    //        int IntegerMethod(string s);
    //        object ObjectMethod(int i);
    //    }

    //    protected override void Establish_context()
    //    {
    //        base.Establish_context();

    //        _stub = CreateDependency<ISampleClass>();
    //    }

    //    [Test]
    //    public void Calling_void_methods_will_do_nothing()
    //    {
    //        _stub.VoidMethod();
    //    }

    //    [Test]
    //    public void Calling_methods_that_return_value_types_will_return_the_default_value_for_that_type()
    //    {
    //        _stub.IntegerMethod("foo").ShouldEqual(0);
    //    }

    //    [Test]
    //    public void Calling_methods_that_return_reference_types_will_return_null()
    //    {
    //        _stub.ObjectMethod(1).ShouldBeNull();
    //    }

    //    [Test]
    //    public void You_can_tell_the_mock_what_value_to_return_for_a_method()
    //    {
    //        // calling the method with "foo" as the parameter will return 5
    //        _stub.Stub(s => s.IntegerMethod("foo")).Return(5);

    //        // calling the method with any other parameter will return 10
    //        _stub.Stub(s => s.IntegerMethod(null)).IgnoreArguments().Return(10);

    //        _stub.IntegerMethod("foo").ShouldEqual(5);
    //        _stub.IntegerMethod("iojaewfj").ShouldEqual(10);
    //        _stub.IntegerMethod(null).ShouldEqual(10);
    //    }

    //    [Test]
    //    public void You_can_tell_the_mock_to_throw_an_exception_when_a_method_is_called()
    //    {
    //        // calling the method with "foo" as the parameter will throw exception
    //        _stub.Stub(s => s.IntegerMethod("foo")).Throw(new InvalidOperationException());

    //        // calling the method with any other parameter will return 10
    //        _stub.Stub(s => s.IntegerMethod(null)).IgnoreArguments().Return(10);

    //        typeof(InvalidOperationException).ShouldBeThrownBy(
    //            () => _stub.IntegerMethod("foo").ShouldEqual(5));

    //        _stub.IntegerMethod("iojaewfj").ShouldEqual(10);
    //        _stub.IntegerMethod(null).ShouldEqual(10);
    //    }

    //    [Test]
    //    public void You_can_check_to_see_if_a_method_was_called()
    //    {
    //        _stub.IntegerMethod("foo");

    //        _stub.AssertWasCalled(s => s.IntegerMethod("foo"));
    //        _stub.AssertWasCalled(s => s.IntegerMethod(null), o => o.IgnoreArguments());
    //        _stub.AssertWasCalled(s => s.IntegerMethod("foo"), o => o.Repeat.Once());
    //    }

    //    [Test]
    //    public void You_can_check_to_see_if_a_method_was_not_called()
    //    {
    //        _stub.IntegerMethod("foo");

    //        _stub.AssertWasNotCalled(s => s.IntegerMethod("asdfdsf"));
    //        _stub.AssertWasNotCalled(s => s.VoidMethod(), o => o.IgnoreArguments());
    //    }

    //    [Test]
    //    public void You_can_get_the_arguments_of_calls_to_a_method()
    //    {
    //        _stub.IntegerMethod("foo");

    //        IList<object[]> argsPerCall = _stub.GetArgumentsForCallsMadeOn(s => s.IntegerMethod(null));
    //        argsPerCall[0][0].ShouldEqual("foo");
    //    }

    //    [Test]
    //    public void You_have_to_stub_the_properties_if_you_want_it_to_return_a_value()
    //    {
    //        _stub.Stub(s => s.Property).Return("foo");
    //        _stub.Property.ShouldEqual("foo");
    //    }

    //    [Test]
    //    public void If_you_set_a_property_it_will_NOT_return_the_value_that_you_set()
    //    {
    //        _stub.Property = "foo";
    //        _stub.Property.ShouldBeNull();
    //    }
    //}

    //public class When_working_with_a_mock_of_a_concrete_class : SpecBase
    //{
    //    private SampleClass _class;

    //    public class SampleClass
    //    {
    //        private string _property;
    //        public string Property
    //        {
    //            get { return _property; }
    //            set
    //            {
    //                _property = value;
    //                PropertyWasSet = true;
    //            }
    //        }

    //        private string _virtualProperty;
    //        public string VirtualProperty
    //        {
    //            get { return _virtualProperty; }
    //            set
    //            {
    //                _virtualProperty = value;
    //                VirtualPropertyWasSet = true;
    //            }
    //        }

    //        public bool PropertyWasSet { get; set; }
    //        public bool VirtualPropertyWasSet { get; set; }
    //        public bool VoidMethodWasCalled { get; set; }
    //        public bool IntegerMethodWasCalled { get; set; }
    //        public bool ObjectMethodWasCalled { get; set; }

    //        public void VoidMethod()
    //        {
    //            VoidMethodWasCalled = true;
    //        }

    //        public virtual int IntegerMethod(string s)
    //        {
    //            IntegerMethodWasCalled = true;
    //            return s.Length;
    //        }

    //        public object ObjectMethod(int i)
    //        {
    //            ObjectMethodWasCalled = true;
    //            return new List<int> { i };
    //        }
    //    }

    //    protected override void Establish_context()
    //    {
    //        base.Establish_context();

    //        _class = CreateDependency<SampleClass>();
    //    }

    //    [Test]
    //    public void Calling_non_virtual_methods_will_call_the_actual_method()
    //    {
    //        _class.VoidMethod();
    //        _class.VoidMethodWasCalled.ShouldBeTrue();
    //    }

    //    [Test]
    //    public void Virtual_methods_are_stubbed_unless_you_give_it_a_value_to_return()
    //    {
    //        _class.IntegerMethod("foo").ShouldEqual(0);
    //        _class.IntegerMethodWasCalled.ShouldBeFalse();
    //        _class.AssertWasCalled(c => c.IntegerMethod("foo"));
    //    }

    //    [Test]
    //    public void You_can_stub_a_virtual_method()
    //    {
    //        _class.Stub(c => c.IntegerMethod("foo")).Return(100);

    //        _class.IntegerMethod("foo").ShouldEqual(100);
    //    }

    //    [Test]
    //    public void You_can_have_virtual_methods_throw_an_exception_when_they_are_called()
    //    {
    //        _class.Stub(c => c.IntegerMethod("foo")).Throw(new InvalidOperationException());

    //        typeof(InvalidOperationException).ShouldBeThrownBy(
    //            () => _class.IntegerMethod("foo"));
    //    }

    //    [Test]
    //    public void You_cannot_stub_a_non_virtual_method()
    //    {
    //        typeof(Exception).ShouldBeThrownBy(
    //            () => _class.Stub(s => s.ObjectMethod(1)).Return("foo"));
    //    }

    //    [Test]
    //    public void You_can_check_to_see_if_a_virtual_method_was_called()
    //    {
    //        _class.IntegerMethod("foo");

    //        _class.AssertWasCalled(s => s.IntegerMethod("foo"));
    //        _class.AssertWasCalled(s => s.IntegerMethod(null), o => o.IgnoreArguments());
    //        _class.AssertWasCalled(s => s.IntegerMethod("foo"), o => o.Repeat.Once());
    //    }

    //    [Test]
    //    public void You_cannot_use_AssertWasCalled_on_a_non_virtual_method()
    //    {
    //        _class.VoidMethod();

    //        typeof(Exception).ShouldBeThrownBy(
    //            () => _class.AssertWasCalled(s => s.VoidMethod()));
    //    }

    //    [Test]
    //    public void You_can_check_to_see_if_a_virtual_method_was_not_called()
    //    {
    //        _class.AssertWasNotCalled(s => s.IntegerMethod("foo"));
    //        _class.AssertWasNotCalled(s => s.IntegerMethod(null), o => o.IgnoreArguments());
    //    }

    //    [Test]
    //    public void You_cannot_use_AssertWasNotCalled_on_a_non_virtual_method()
    //    {
    //        typeof(Exception).ShouldBeThrownBy(
    //            () => _class.AssertWasNotCalled(s => s.VoidMethod()));
    //    }

    //    [Test]
    //    public void You_cannot_get_the_arguments_of_calls_to_a_non_virtual_method()
    //    {
    //        _class.ObjectMethod(1);

    //        typeof(Exception).ShouldBeThrownBy(
    //            () => _class.GetArgumentsForCallsMadeOn(s => s.ObjectMethod(0)));
    //    }

    //    [Test]
    //    public void You_can_get_the_arguments_of_calls_to_a_virtual_method()
    //    {
    //        _class.IntegerMethod("foo");

    //        IList<object[]> argsPerCall = _class.GetArgumentsForCallsMadeOn(s => s.IntegerMethod("foo"));
    //        argsPerCall[0][0].ShouldEqual("foo");
    //    }

    //    [Test]
    //    public void Non_virtual_properties_work_as_normal()
    //    {
    //        _class.Property = "foo";
    //        _class.Property.ShouldEqual("foo");
    //    }

    //    [Test]
    //    public void Virtual_properties_work_as_normal()
    //    {
    //        _class.VirtualProperty = "foo";
    //        _class.VirtualProperty.ShouldEqual("foo");
    //    }
    //}

    //public class When_working_with_a_partial_mock_of_a_concrete_class : SpecBase
    //{
    //    private SampleClass _class;

    //    public class SampleClass
    //    {
    //        private string _property;
    //        public string Property
    //        {
    //            get { return _property; }
    //            set
    //            {
    //                _property = value;
    //                PropertyWasSet = true;
    //            }
    //        }

    //        private string _virtualProperty;
    //        public string VirtualProperty
    //        {
    //            get { return _virtualProperty; }
    //            set
    //            {
    //                _virtualProperty = value;
    //                VirtualPropertyWasSet = true;
    //            }
    //        }

    //        public bool PropertyWasSet { get; set; }
    //        public bool VirtualPropertyWasSet { get; set; }
    //        public bool VoidMethodWasCalled { get; set; }
    //        public bool IntegerMethodWasCalled { get; set; }
    //        public bool ObjectMethodWasCalled { get; set; }

    //        public void VoidMethod()
    //        {
    //            VoidMethodWasCalled = true;
    //        }

    //        public virtual int MethodThatReturnsInteger(string s)
    //        {
    //            IntegerMethodWasCalled = true;
    //            return s.Length;
    //        }

    //        public object MethodThatReturnsObject(int i)
    //        {
    //            ObjectMethodWasCalled = true;
    //            return new List<int> { i };
    //        }
    //    }

    //    protected override void Establish_context()
    //    {
    //        base.Establish_context();

    //        _class = CreatePartialMock<SampleClass>();
    //    }

    //    [Test]
    //    public void Calling_non_virtual_methods_will_call_the_actual_method()
    //    {
    //        _class.VoidMethod();
    //        _class.VoidMethodWasCalled.ShouldBeTrue();
    //    }

    //    [Test]
    //    public void Calling_virtual_methods_will_call_the_actual_method()
    //    {
    //        _class.MethodThatReturnsInteger("foo").ShouldEqual(3);
    //        _class.IntegerMethodWasCalled.ShouldBeTrue();
    //        _class.AssertWasCalled(c => c.MethodThatReturnsInteger("foo"));
    //    }

    //    [Test]
    //    public void You_can_stub_a_virtual_method()
    //    {
    //        _class.Stub(c => c.MethodThatReturnsInteger("foo")).Return(100);

    //        _class.MethodThatReturnsInteger("foo").ShouldEqual(100);
    //        _class.AssertWasCalled(c => c.MethodThatReturnsInteger("foo"));
    //    }

    //    [Test]
    //    public void You_can_have_virtual_methods_throw_an_exception_when_they_are_called()
    //    {
    //        _class.Stub(c => c.MethodThatReturnsInteger("foo")).Throw(new InvalidOperationException());

    //        typeof(InvalidOperationException).ShouldBeThrownBy(
    //            () => _class.MethodThatReturnsInteger("foo"));
    //    }
    //    [Test]
    //    public void You_cannot_stub_a_non_virtual_method()
    //    {
    //        typeof(Exception).ShouldBeThrownBy(
    //            () => _class.Stub(s => s.MethodThatReturnsObject(1)).Return("foo"));
    //    }

    //    [Test]
    //    public void You_can_check_to_see_if_a_virtual_method_was_called()
    //    {
    //        _class.MethodThatReturnsInteger("foo");

    //        _class.AssertWasCalled(s => s.MethodThatReturnsInteger("foo"));
    //        _class.AssertWasCalled(s => s.MethodThatReturnsInteger(null), o => o.IgnoreArguments());
    //        _class.AssertWasCalled(s => s.MethodThatReturnsInteger("foo"), o => o.Repeat.Once());
    //    }

    //    [Test]
    //    public void You_cannot_use_AssertWasCalled_on_a_non_virtual_method()
    //    {
    //        _class.VoidMethod();

    //        typeof(Exception).ShouldBeThrownBy(
    //            () => _class.AssertWasCalled(s => s.VoidMethod()));
    //    }

    //    [Test]
    //    public void You_can_check_to_see_if_a_non_virtual_method_was_not_called()
    //    {
    //        _class.AssertWasNotCalled(s => s.MethodThatReturnsInteger("foo"));
    //        _class.AssertWasNotCalled(s => s.MethodThatReturnsInteger(null), o => o.IgnoreArguments());
    //    }

    //    [Test]
    //    public void You_cannot_use_AssertWasNotCalled_on_a_non_virtual_method()
    //    {
    //        typeof(Exception).ShouldBeThrownBy(
    //            () => _class.AssertWasNotCalled(s => s.VoidMethod()));
    //    }

    //    [Test]
    //    public void You_cannot_get_the_arguments_of_calls_to_a_non_virtual_method()
    //    {
    //        _class.MethodThatReturnsObject(1);

    //        typeof(Exception).ShouldBeThrownBy(
    //            () => _class.GetArgumentsForCallsMadeOn(s => s.MethodThatReturnsObject(0)));
    //    }

    //    [Test]
    //    public void You_can_get_the_arguments_of_calls_to_a_virtual_method()
    //    {
    //        _class.MethodThatReturnsInteger("foo");

    //        IList<object[]> argsPerCall = _class.GetArgumentsForCallsMadeOn(s => s.MethodThatReturnsInteger("foo"));
    //        argsPerCall[0][0].ShouldEqual("foo");
    //    }

    //    [Test]
    //    public void Non_virtual_properties_work_as_normal()
    //    {
    //        _class.Property = "foo";
    //        _class.Property.ShouldEqual("foo");
    //    }

    //    [Test]
    //    public void Virtual_properties_work_as_normal()
    //    {
    //        _class.VirtualProperty = "foo";
    //        _class.VirtualProperty.ShouldEqual("foo");
    //    }
    //}
}