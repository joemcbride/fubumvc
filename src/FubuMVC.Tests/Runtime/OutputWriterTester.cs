using System;
using System.IO;
using System.Net;
using System.Web;
using FubuCore;
using FubuCore.Logging;
using FubuMVC.Core.Caching;
using FubuMVC.Core.Http;
using FubuMVC.Core.Runtime;
using FubuMVC.Core.Runtime.Logging;
using FubuMVC.Tests.TestSupport;
using Shouldly;
using NUnit.Framework;
using Rhino.Mocks;
using System.Linq;
using System.Threading.Tasks;
using FubuMVC.Core.ServiceBus;
using Cookie = FubuMVC.Core.Http.Cookies.Cookie;

namespace FubuMVC.Tests.Runtime
{
    [TestFixture]
    public class OutputWriterTester : InteractionContext<OutputWriter>
    {
        private IHttpResponse theHttpResponse;
        private RecordingLogger logs;

        protected override void beforeEach()
        {
            theHttpResponse = MockFor<IHttpResponse>();

            logs = RecordLogging();
        }

        [Test]
        public void flush_delegates()
        {
            ClassUnderTest.Flush();
            theHttpResponse.AssertWasCalled(x => x.Flush());
        }

        [Test]
        public void redirect_to_a_url_delegates()
        {
            ClassUnderTest.RedirectToUrl("http://somewhere.com");

            theHttpResponse.AssertWasCalled(x => x.Redirect("http://somewhere.com"));
        }

        [Test]
        public void redirect_to_url_logs_the_redirection()
        {
            ClassUnderTest.RedirectToUrl("http://somewhere.com");

            logs.DebugMessages.Single().ShouldBe(new RedirectReport("http://somewhere.com"));
        }

        [Test]
        public void write_in_normal_mode_delegates_to_http_writer()
        {
            ClassUnderTest.Write("text/json", "{}");

            theHttpResponse.AssertWasCalled(x => x.Write("{}"));
            theHttpResponse.AssertWasCalled(x => x.WriteContentType("text/json"));
        }

        [Test]
        public void write_in_normal_mode_records_content_type_and_text()
        {
            ClassUnderTest.Write("text/json", "{}");

            logs.DebugMessages.Single().ShouldBe(new OutputReport("text/json", "{}"));
        }

        [Test]
        public void write_records_content_type_and_text()
        {
            ClassUnderTest.Write("Some text");

            logs.DebugMessages.Single().ShouldBe(new OutputReport("Some text"));
        }

        [Test]
        public void write_response_code_delegates()
        {
            ClassUnderTest.WriteResponseCode(HttpStatusCode.UseProxy);

            theHttpResponse.AssertWasCalled(x => x.WriteResponseCode(HttpStatusCode.UseProxy));
        }

        [Test]
        public void write_response_code_and_description_delegates()
        {
            const string description = "why u no make good request?";
            ClassUnderTest.WriteResponseCode(HttpStatusCode.BadRequest, description);
            theHttpResponse.AssertWasCalled(x => x.WriteResponseCode(HttpStatusCode.BadRequest, description));
        }

        [Test]
        public void write_by_stream_delegates_to_the_http_writer_in_normal_mode()
        {
            Func<Stream, Task> action = stream => Task.CompletedTask;

            theHttpResponse.Stub(x => x.Write(action)).Return(Task.CompletedTask);

            ClassUnderTest.Write("text/json",  action).GetAwaiter().GetResult();

            theHttpResponse.AssertWasCalled(x => x.WriteContentType("text/json"));
            theHttpResponse.AssertWasCalled(x => x.Write((Func<Stream, Task>) action));
        }

        [Test]
        public void append_header_writes_directly_to_the_ihttpwriter_in_normal_mode()
        {
            ClassUnderTest.AppendHeader("e-tag", "12345");

            theHttpResponse.AssertWasCalled(x => x.AppendHeader("e-tag", "12345"));

        }

        [Test]
        public void replay_logs()
        {
            var recording = MockRepository.GenerateMock<IRecordedOutput>();

            ClassUnderTest.Replay(recording);

            logs.DebugMessages.Single().ShouldBe(new ReplayRecordedOutput(recording));
        }

        [Test]
        public void flushing_the_output_records_a_debug_message()
        {
            ClassUnderTest.Flush();

            logs.DebugMessages.Single().ShouldBeOfType<StringMessage>()
                .Message.ShouldBe("Flushed content to the Http output");
        }

        [Test]
        public void write_file_logs()
        {
            ClassUnderTest.WriteFile(MimeType.Jpg, "file path", "some display");

            logs.DebugMessages.Single().ShouldBe(new FileOutputReport{
                ContentType = MimeType.Jpg.Value,
                DisplayName = "some display",
                LocalFilePath = "file path"
            });
        }

        [Test]
        public void writing_a_response_code_will_log()
        {
            const string description = "why u no make good request?";
            ClassUnderTest.WriteResponseCode(HttpStatusCode.BadRequest, description);

            logs.DebugMessages.Single().ShouldBe(new HttpStatusReport{
                Description = description,
                Status = HttpStatusCode.BadRequest
            });
        }

        [Test]
        public void write_cookie()
        {
            var cookie = new Cookie("something", "else");

            ClassUnderTest.AppendCookie(cookie);

            logs.DebugMessages.Single().ShouldBe(new WriteCookieReport(cookie));
        }

        [Test]
        public void recording_writes_a_report()
        {
            ClassUnderTest.Record(() =>
            {
                ClassUnderTest.Write("some stuff");
                return Task.CompletedTask;
            }).Wait();

            logs.DebugMessages.Count().ShouldBe(3);
            logs.DebugMessages.First().ShouldBeOfType<StartedRecordingOutput>();
            logs.DebugMessages.ElementAt(1).ShouldBeOfType<OutputReport>().Contents.ShouldBe("some stuff");
            logs.DebugMessages.Last().ShouldBeOfType<FinishedRecordingOutput>();
        }

        [Test]
        public void AppendHeader_records_any_values()
        {
            ClassUnderTest.AppendHeader("something", "a value");

            logs.DebugMessages.Single().ShouldBe(new SetHeaderValue("something", "a value"));
        }

        [Test]
        public void write_stream_logs()
        {
            Func<Stream, Task> action = stream => Task.CompletedTask;

            theHttpResponse.Stub(x => x.Write(action)).Return(Task.CompletedTask);
            
            ClassUnderTest.Write("text/xml", action).GetAwaiter().GetResult();

            logs.DebugMessages.Single().ShouldBe(new WriteToStreamReport("text/xml"));
        }

        [Test]
        public void dispose_does_not_flush()
        {
            Services.PartialMockTheClassUnderTest();
            ClassUnderTest.Dispose();
            ClassUnderTest.AssertWasNotCalled(x => x.Flush());
        }
    }

    [TestFixture]
    public class when_writing_within_recorded_output_mode : InteractionContext<OutputWriter>
    {
        private string theContent;
        private string theNestedContent;

        private string theContentType;

        private RecordedOutput theRecordedOutput;
        private RecordedOutput theNestedOutput;

        protected override void beforeEach()
        {
            theContent = "some content";
            theNestedContent = "nested content";
            theContentType = "text/xml";

            theRecordedOutput = ClassUnderTest.Record(() =>
            {
                ClassUnderTest.Write(theContentType, theContent);
                theNestedOutput = ClassUnderTest.Record(() =>
                {
                    ClassUnderTest.Write(theContentType, theNestedContent);
                    return Task.CompletedTask;
                }).GetAwaiter().GetResult().As<RecordedOutput>();

                return Task.CompletedTask;

            }).GetAwaiter().GetResult().As<RecordedOutput>();
        }

        [Test]
        public void recorded_output_should_have_what_was_written()
        {
            theRecordedOutput.Outputs
                .ShouldHaveTheSameElementsAs(new SetContentType(theContentType), new WriteTextOutput(theContent));
        }

        [Test]
        public void should_not_have_written_directly_to_the_http_writer()
        {
            MockFor<IHttpResponse>().AssertWasNotCalled(x => x.Write(theContent));
            MockFor<IHttpResponse>().AssertWasNotCalled(x => x.WriteContentType(theContentType));
        }

        [Test]
        public void should_restore_to_normal_writing_after_recording()
        {
            ClassUnderTest.Write(theContentType, theContent);

            MockFor<IHttpResponse>().AssertWasCalled(x => x.Write(theContent));
            MockFor<IHttpResponse>().AssertWasCalled(x => x.WriteContentType(theContentType));
        }

        [Test]
        public void should_nest_correctly_when_recording_1()
        {
            theNestedOutput.GetText().ShouldBe(theNestedContent);
        }

        [Test]
        public void should_nest_correctly_when_recording_2()
        {
            ClassUnderTest.Record(() =>
            {
                ClassUnderTest.WriteHtml("Monty");
                var nested = ClassUnderTest.Record(() =>
                {
                    ClassUnderTest.WriteHtml("Python");
                    return Task.CompletedTask;
                });
                ClassUnderTest.WriteHtml(nested.GetAwaiter().GetResult().GetText());

                return Task.CompletedTask;

            }).GetAwaiter().GetResult().GetText().ShouldBe("MontyPython");
        }
    }

    [TestFixture]
    public class when_writing_a_file_with_a_display_name : InteractionContext<OutputWriter>
    {
        private string theDisplayName;
        private string theFilePath;
        private string theContentType;

        protected override void beforeEach()
        {
            theDisplayName = "The title";
            theFilePath = "location";
            theContentType = "content type";

            MockFor<IFileSystem>().Stub(x => x.FileSizeOf(theFilePath)).Return(123);

            ClassUnderTest.WriteFile(theContentType, theFilePath, theDisplayName);
        }

        [Test]
        public void should_actually_you_know_write_the_file_itself()
        {
            MockFor<IHttpResponse>().AssertWasCalled(x => x.WriteFile(theFilePath));
        }

        [Test]
        public void should_have_written_the_content_type()
        {
            MockFor<IHttpResponse>().AssertWasCalled(x => x.WriteContentType(theContentType));
        }

        [Test]
        public void should_write_a_content_disposition_header_for_the_display()
        {
            MockFor<IHttpResponse>().AssertWasCalled(
                x => x.AppendHeader("Content-Disposition", "attachment; filename=\"The title\""));
        }

        [Test]
        public void should_write_header_for_content_length()
        {
            MockFor<IHttpResponse>().AssertWasCalled(x => x.AppendHeader("Content-Length", "123"));
        }
    }
}