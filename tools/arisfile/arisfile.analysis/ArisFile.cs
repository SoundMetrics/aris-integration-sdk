using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace arisfile.analysis
{
    public class ArisFile
    {
        public ArisFile(Stream stream)
        {
            this.stream = stream;
        }

        public IEnumerable<ArisFrameAccessor> Frames
        {
            get { return stream.EnumerateArisFrames(); }
        }

        private class FramesEnumerable : IEnumerable<ArisFrameAccessor>
        {
            public IEnumerator<ArisFrameAccessor> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        private readonly Stream stream;
    }
}
