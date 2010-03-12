using NBehave.Spec.NUnit;
using NUnit.Framework;
using Rhino.Mocks;
using System.Collections.Generic;
using System;
using Is = Rhino.Mocks.Constraints.Is;
using Rhino.Mocks.Exceptions;

namespace RhinoMocksExamples
{
    // Here are some tests that will show what how to do everything
    // you would want to do with Rhino Mocks.  You can also read the 
    // documentation (which is pretty good) here:
    // http://ayende.com/Wiki/Rhino+Mocks+3.5.ashx

    // The Rhino Mocks syntax has changed drastically over the years,
    // definitely for the good.  You may find old blog posts out there
    // showing how to do it the old way.  Just ignore them and be 
    // glad that you didn't have to do it the old way.
    
    // Let's create some sample classes that we'll work with.

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

    public class SampleClass
    {
        private string _nonVirtualProperty;
        public string NonVirtualProperty
        {
            get { return _nonVirtualProperty; }
            set
            {
                _nonVirtualProperty = value;
                NonVirtualPropertyWasSet = true;
            }
        }

        private string _virtualProperty;
        public virtual string VirtualProperty
        {
            get { return _virtualProperty; }
            set
            {
                _virtualProperty = value;
                VirtualPropertyWasSet = true;
            }
        }

        public string SetByConstructor { get; private set; }
        public bool NonVirtualPropertyWasSet { get; set; }
        public bool VirtualPropertyWasSet { get; set; }
        public bool VoidMethodWasCalled { get; set; }
        public bool VirtualMethodWasCalled { get; set; }
        public bool NonVirtualMethodWasCalled { get; set; }

        public event EventHandler SomeEvent;
        public virtual event EventHandler SomeVirtualEvent;

        public SampleClass()
        {
            
        }

        public SampleClass(string value)
        {
            SetByConstructor = value;
        }

        public void VoidMethod()
        {
            VoidMethodWasCalled = true;
        }

        public virtual int VirtualMethod(string s)
        {
            VirtualMethodWasCalled = true;
            return s.Length;
        }

        public IList<int> NonVirtualMethod(int i)
        {
            NonVirtualMethodWasCalled = true;
            return new List<int> { i };
        }

        public void FireSomeVirtualEvent()
        {
            if (SomeVirtualEvent != null)   
                SomeVirtualEvent(this, EventArgs.Empty);
        }
    }

    // Time for tests.

    public class When_working_with_a_stub_of_an_interface : SpecBase
    {
        // "stub" == "fake"

        [Test]
        public void You_can_create_a_stub_by_calling_MockRepository_GenerateStub()
        {
            // This is the out-of-the-box way to create a stub in Rhino Mocks.
            // Rhino Mocks will dynamically create a class that implements 
            // ISampleClass.
            var stub = MockRepository.GenerateStub<ISampleClass>();
        }

        [Test]
        public void NBehave_gives_us_a_shorthand_way_of_creating_stubs()
        {
            // Less typing.  
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

            // some other options
            stub.AssertWasCalled(s => s.MethodThatReturnsInteger("foo"), o => o.Repeat.Times(2));
            stub.AssertWasCalled(s => s.MethodThatReturnsInteger("foo"), o => o.Repeat.AtLeastOnce());
            stub.AssertWasCalled(s => s.MethodThatReturnsInteger("foo"), o => o.Repeat.Twice()); 
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

        [Test]
        public void If_you_set_a_property_the_getter_will_return_the_value()
        {
            var stub = CreateStub<ISampleClass>();

            stub.Property = "foo";
            stub.Property.ShouldEqual("foo");
        }

        [Test]
        public void You_cannot_use_AssertWasCalled_with_properties_on_a_stub()
        {
            // But why would you need to?  You can just get the value 
            // directly from the property.
            var stub = CreateStub<ISampleClass>();

            stub.Property = "foo";

            // Don't do this
            //stub.AssertWasCalled(s => s.Property);

            // Just do this
            stub.Property.ShouldEqual("foo");
        }

        [Test]
        public void You_can_tell_events_on_a_stub_to_fire()
        {
            var stub = CreateStub<ISampleClass>();
            var eventWasHandled = false;

            // attach an event handler
            stub.SomeEvent += (args, e) => eventWasHandled = true;

            // raise the event
            stub.Raise(s => s.SomeEvent += null, this, EventArgs.Empty);

            eventWasHandled.ShouldBeTrue();
        }
    }

    public class When_working_with_a_mock_of_an_interface : SpecBase
    {
        // You can do pretty much everything with stubs.  I don't see a reason
        // to ever use mocks.  If you want to know the technical academic difference
        // between a mock and a stub, you can read about it here:
        // http://martinfowler.com/articles/mocksArentStubs.html
        //
        // Personally I think it's all semantics and that it doesn't really matter.
        // I'd recommend just using stubs with Rhino Mocks.  But if you really care,
        // here are the things that are different with mocks.

        [Test]
        public void You_can_create_a_stub_by_calling_MockRepository_GenerateMock()
        {
            var mock = MockRepository.GenerateMock<ISampleClass>();
        }

        [Test]
        public void NBehave_gives_us_a_shorthand_way_of_creating_mocks()
        {
            // Less typing.  
            var mock = CreateDependency<ISampleClass>();
        }

        [Test]
        public void You_can_check_to_see_if_a_property_was_set()
        {
            var mock = CreateDependency<ISampleClass>();

            mock.Property = "foo";

            mock.AssertWasCalled(s => s.Property = "foo");
        }

        [Test]
        public void You_can_check_to_see_if_a_property_getter_was_called()
        {
            var mock = CreateDependency<ISampleClass>();

            var value = mock.Property;

            mock.AssertWasCalled(s => { var ignored = s.Property; });
        }
    }

    public class When_working_with_a_partial_mock_of_a_concrete_class : SpecBase
    {
        [Test]
        public void Here_is_how_you_create_a_partial_mock()
        {
            var mockRepository = new MockRepository();
            var sampleClass = mockRepository.PartialMock<SampleClass>();
            sampleClass.Replay();

            // There is currently no special NBehave way to create a partial mock.
        }

        [Test]
        public void You_can_pass_in_constuctor_arguments_and_Rhino_Mocks_will_pick_the_constructor_that_fits()
        {
            var mockRepository = new MockRepository();
            var sampleClass = mockRepository.PartialMock<SampleClass>("foo");
            sampleClass.Replay();

            sampleClass.SetByConstructor.ShouldEqual("foo");
        }

        [Test]
        public void Calling_non_virtual_methods_will_call_the_actual_method()
        {
            var mockRepository = new MockRepository();
            var sampleClass = mockRepository.PartialMock<SampleClass>();
            sampleClass.Replay();

            sampleClass.VoidMethod();
            sampleClass.VoidMethodWasCalled.ShouldBeTrue();
        }

        [Test]
        public void Calling_virtual_methods_will_call_the_actual_method()
        {
            var mockRepository = new MockRepository();
            var sampleClass = mockRepository.PartialMock<SampleClass>();
            sampleClass.Replay();

            sampleClass.VirtualMethod("foo").ShouldEqual(3);
            sampleClass.VirtualMethodWasCalled.ShouldBeTrue();
            sampleClass.AssertWasCalled(c => c.VirtualMethod("foo"));
        }

        [Test]
        public void You_can_stub_a_virtual_method_and_give_it_a_return_value()
        {
            var mockRepository = new MockRepository();
            var sampleClass = mockRepository.PartialMock<SampleClass>();
            sampleClass.Replay();

            sampleClass.Stub(c => c.VirtualMethod("foo")).Return(100);

            sampleClass.VirtualMethod("foo").ShouldEqual(100);

            // It's not actually going to run the real method since we stubbed it out.
            sampleClass.VirtualMethodWasCalled.ShouldBeFalse();

            sampleClass.AssertWasCalled(c => c.VirtualMethod("foo"));
        }

        [Test]
        public void You_can_have_virtual_methods_throw_an_exception_when_they_are_called()
        {
            var mockRepository = new MockRepository();
            var sampleClass = mockRepository.PartialMock<SampleClass>();

            sampleClass.Stub(c => c.VirtualMethod("foo")).Throw(new InvalidOperationException());

            sampleClass.Replay();

            typeof(InvalidOperationException).ShouldBeThrownBy(
                () => sampleClass.VirtualMethod("foo"));
        }

        [Test]
        public void You_cannot_stub_a_non_virtual_method()
        {
            var mockRepository = new MockRepository();
            var sampleClass = mockRepository.PartialMock<SampleClass>();

            typeof(Exception).ShouldBeThrownBy(
                () => sampleClass.Stub(s => s.NonVirtualMethod(1)).Return(new List<int> { 3 }));
        }

        [Test]
        public void You_can_check_to_see_if_a_virtual_method_was_called()
        {
            var mockRepository = new MockRepository();
            var sampleClass = mockRepository.PartialMock<SampleClass>();
            sampleClass.Replay();

            sampleClass.VirtualMethod("foo");

            sampleClass.AssertWasCalled(s => s.VirtualMethod("foo"));
            sampleClass.AssertWasCalled(s => s.VirtualMethod(Arg<string>.Is.Anything));
            sampleClass.AssertWasCalled(s => s.VirtualMethod("foo"), o => o.Repeat.Once());
        }

        [Test]
        public void You_cannot_use_AssertWasCalled_on_a_non_virtual_method()
        {
            var mockRepository = new MockRepository();
            var sampleClass = mockRepository.PartialMock<SampleClass>();
            sampleClass.Replay();

            sampleClass.VoidMethod();

            typeof(Exception).ShouldBeThrownBy(
                () => sampleClass.AssertWasCalled(s => s.VoidMethod()));
        }

        [Test]
        public void You_cannot_use_AssertWasNotCalled_on_a_non_virtual_method()
        {
            var mockRepository = new MockRepository();
            var sampleClass = mockRepository.PartialMock<SampleClass>();
            sampleClass.Replay();

            typeof (Exception).ShouldBeThrownBy(
                () => sampleClass.AssertWasNotCalled(s => s.NonVirtualMethod(1)));
        }

        [Test]
        public void You_can_get_the_arguments_of_calls_to_a_virtual_method()
        {
            var mockRepository = new MockRepository();
            var sampleClass = mockRepository.PartialMock<SampleClass>();
            sampleClass.Replay();

            sampleClass.VirtualMethod("foo");

            IList<object[]> argsPerCall = sampleClass.GetArgumentsForCallsMadeOn(s => s.VirtualMethod("foo"));
            argsPerCall[0][0].ShouldEqual("foo");
        }

        [Test]
        public void You_cannot_get_the_arguments_of_calls_to_a_non_virtual_method()
        {
            var mockRepository = new MockRepository();
            var sampleClass = mockRepository.PartialMock<SampleClass>();
            sampleClass.Replay();

            sampleClass.NonVirtualMethod(1);

            typeof(Exception).ShouldBeThrownBy(
                () => sampleClass.GetArgumentsForCallsMadeOn(s => s.NonVirtualMethod(0)));
        }

        [Test]
        public void Non_virtual_properties_work_as_normal()
        {
            var mockRepository = new MockRepository();
            var sampleClass = mockRepository.PartialMock<SampleClass>();
            sampleClass.Replay();

            sampleClass.NonVirtualProperty = "foo";
            sampleClass.NonVirtualProperty.ShouldEqual("foo");
        }

        [Test]
        public void Virtual_properties_work_as_normal()
        {
            var mockRepository = new MockRepository();
            var sampleClass = mockRepository.PartialMock<SampleClass>();
            sampleClass.Replay();

            sampleClass.VirtualProperty = "foo";
            sampleClass.VirtualProperty.ShouldEqual("foo");
        }

        [Test]
        public void You_can_tell_virtual_events_on_a_partial_mock_to_fire()
        {
            var mockRepository = new MockRepository();
            var sampleClass = mockRepository.PartialMock<SampleClass>();
            sampleClass.Replay();

            var eventWasHandled = false;

            // attach an event handler
            sampleClass.SomeVirtualEvent += (args, e) => eventWasHandled = true;

            // raise the event
            sampleClass.Raise(s => s.SomeVirtualEvent += null, this, EventArgs.Empty);

            eventWasHandled.ShouldBeTrue();
        }

        [Test]
        public void Non_virtual_events_work_normally()
        {
            var mockRepository = new MockRepository();
            var sampleClass = mockRepository.PartialMock<SampleClass>();
            sampleClass.Replay();

            var eventWasHandled = false;

            // attach an event handler
            sampleClass.SomeVirtualEvent += (args, e) => eventWasHandled = true;

            // raise the event
            sampleClass.FireSomeVirtualEvent();

            eventWasHandled.ShouldBeTrue();
        }
    }
}