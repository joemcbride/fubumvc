using System.Collections.Generic;
using FubuMVC.Core.Behaviors.Conditional;

namespace FubuMVC.Core.Resources.Conneg.New
{
    public class Media<T> : IMedia<T>
    {
        // TODO -- make this lazy some day


        private readonly IMediaWriter<T> _writer;
        private readonly IConditional _condition;

        public Media(IMediaWriter<T> writer, IConditional condition)
        {
            _writer = writer;
            _condition = condition;
        }

        public IEnumerable<string> Mimetypes
        {
            get { return _writer.Mimetypes; }
        }

        public void Write(string mimeType, T resource)
        {
            _writer.Write(mimeType, resource);
        }

        public bool MatchesRequest()
        {
            return _condition.ShouldExecute();
        }
    }
}