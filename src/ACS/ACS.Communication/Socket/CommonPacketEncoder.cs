using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Filter.Codec;
using Mina.Filter.Codec.TextLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Socket
{
    /// <summary>
    /// Socket first sending part (ES -> AGV)
    /// </summary>
    public class CommonPacketEncoder : IProtocolEncoder
    {
        public void Dispose(IoSession session)
        {
            // Do nothing
        }

        public void Encode(IoSession session, object message, IProtocolEncoderOutput @out)
        {
#if BYTE12
            IoBuffer buffer = IoBuffer.Allocate(12);
            buffer.Put((new SendPacket((byte[])message)).GetRawData());
            buffer.Flip();
            @out.Write(buffer);
            buffer.Clear();
#else
            //190524 Send Packet의 data Length 변경(Fix->Flexible) 에 따른 변경
            //송신Packet의 Message 유동적
            IoBuffer buffer = IoBuffer.Allocate(((byte[])message).Length);
            buffer.Put((new SendPacket((byte[])message)).GetRawData());
            buffer.Flip();
            @out.Write(buffer);
            buffer.Clear();
            //
            //IoBuffer buffer = IoBuffer.Allocate(14);
            //buffer.Put((new SendPacket((byte[])message)).GetRawData());
            //buffer.Flip();
            //@out.Write(buffer);
            //buffer.Clear();
#endif
        }

        //private readonly Encoding _encoding;
        //private readonly LineDelimiter _delimiter;
        //private Int32 _maxLineLength = Int32.MaxValue;

        ///// <summary>
        ///// Instantiates with default <see cref="Encoding.Default"/> and <see cref="LineDelimiter.Unix"/>.
        ///// </summary>
        //public CommonPacketEncoder()
        //    : this(LineDelimiter.Unix)
        //{ }

        ///// <summary>
        ///// Instantiates with default <see cref="Encoding.Default"/> and given delimiter.
        ///// </summary>
        ///// <param name="delimiter">the delimiter string</param>
        //public CommonPacketEncoder(String delimiter)
        //    : this(new LineDelimiter(delimiter))
        //{ }

        ///// <summary>
        ///// Instantiates with default <see cref="Encoding.Default"/> and given delimiter.
        ///// </summary>
        ///// <param name="delimiter">the <see cref="LineDelimiter"/></param>
        //public CommonPacketEncoder(LineDelimiter delimiter)
        //    : this(Encoding.Default, delimiter)
        //{ }

        ///// <summary>
        ///// Instantiates with given encoding,
        ///// and default <see cref="LineDelimiter.Unix"/>.
        ///// </summary>
        ///// <param name="encoding">the <see cref="Encoding"/></param>
        //public CommonPacketEncoder(Encoding encoding)
        //    : this(encoding, LineDelimiter.Unix)
        //{ }

        ///// <summary>
        ///// Instantiates.
        ///// </summary>
        ///// <param name="encoding">the <see cref="Encoding"/></param>
        ///// <param name="delimiter">the delimiter string</param>
        //public CommonPacketEncoder(Encoding encoding, String delimiter)
        //    : this(encoding, new LineDelimiter(delimiter))
        //{ }

        ///// <summary>
        ///// Instantiates.
        ///// </summary>
        ///// <param name="encoding">the <see cref="Encoding"/></param>
        ///// <param name="delimiter">the <see cref="LineDelimiter"/></param>
        //public CommonPacketEncoder(Encoding encoding, LineDelimiter delimiter)
        //{
        //    if (encoding == null)
        //        throw new ArgumentNullException("encoding");
        //    if (delimiter == null)
        //        throw new ArgumentNullException("delimiter");
        //    if (LineDelimiter.Auto.Equals(delimiter))
        //        throw new ArgumentException("AUTO delimiter is not allowed for encoder.");

        //    _encoding = encoding;
        //    _delimiter = delimiter;
        //}

        ///// <summary>
        ///// Gets or sets the allowed maximum size of the encoded line.
        ///// </summary>
        //public Int32 MaxLineLength
        //{
        //    get { return _maxLineLength; }
        //    set
        //    {
        //        if (value <= 0)
        //            throw new ArgumentException("maxLineLength (" + value + ") should be a positive value");
        //        _maxLineLength = value;
        //    }
        //}

        ///// <inheritdoc/>
        //public void Encode(IoSession session, Object message, IProtocolEncoderOutput output)
        //{
        //    String value = message == null ? String.Empty : message.ToString();
        //    value += _delimiter.Value;
        //    Byte[] bytes = _encoding.GetBytes(value);
        //    if (bytes.Length > _maxLineLength)
        //        throw new ArgumentException("Line too long: " + bytes.Length);

        //    // TODO BufferAllocator
        //    IoBuffer buf = IoBuffer.Wrap(bytes);
        //    output.Write(buf);
        //}
    }

}
