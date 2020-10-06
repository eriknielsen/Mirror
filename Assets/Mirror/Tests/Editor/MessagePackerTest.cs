using System;
using NUnit.Framework;
namespace Mirror.Tests
{
    struct TestMessage : NetworkMessage
    {
        public string sceneName;
        // Normal = 0, LoadAdditive = 1, UnloadAdditive = 2
        public SceneOperation sceneOperation;
        public bool customHandling;
    }

    [TestFixture]
    public class MessagePackerTest
    {
        // helper function to pack message into a simple byte[]
        public static byte[] PackToByteArray<T>(T message) where T : struct, NetworkMessage
        {
            using (PooledNetworkWriter writer = NetworkWriterPool.GetWriter())
            {
                MessagePacker.Pack(message, writer);
                return writer.ToArray();
            }
        }

        [Test]
        public void TestPacking()
        {
            TestMessage message = new TestMessage()
            {
                sceneName = "Hello world",
                sceneOperation = SceneOperation.LoadAdditive
            };

            byte[] data = PackToByteArray(message);

            TestMessage unpacked = MessagePacker.Unpack<TestMessage>(data);

            Assert.That(unpacked.sceneName, Is.EqualTo("Hello world"));
            Assert.That(unpacked.sceneOperation, Is.EqualTo(SceneOperation.LoadAdditive));
        }

        [Test]
        public void UnpackWrongMessage()
        {
            ConnectMessage message = new ConnectMessage();

            byte[] data = PackToByteArray(message);

            Assert.Throws<FormatException>(() =>
            {
                DisconnectMessage unpacked = MessagePacker.Unpack<DisconnectMessage>(data);
            });
        }

        [Test]
        public void TestUnpackIdMismatch()
        {
            // Unpack<T> has a id != msgType case that throws a FormatException.
            // let's try to trigger it.

            TestMessage message = new TestMessage()
            {
                sceneName = "Hello world",
                sceneOperation = SceneOperation.LoadAdditive
            };

            byte[] data = PackToByteArray(message);

            // overwrite the id
            data[0] = 0x01;
            data[1] = 0x02;

            Assert.Throws<FormatException>(() =>
            {
                TestMessage unpacked = MessagePacker.Unpack<TestMessage>(data);
            });
        }

        [Test]
        public void TestUnpackMessageNonGeneric()
        {
            // try a regular message
            TestMessage message = new TestMessage()
            {
                sceneName = "Hello world",
                sceneOperation = SceneOperation.LoadAdditive
            };

            byte[] data = PackToByteArray(message);
            NetworkReader reader = new NetworkReader(data);

            bool result = MessagePacker.UnpackMessage(reader, out int msgType);
            Assert.That(result, Is.EqualTo(true));
            Assert.That(msgType, Is.EqualTo(BitConverter.ToUInt16(data, 0)));
        }

        [Test]
        public void UnpackInvalidMessage()
        {
            // try an invalid message
            NetworkReader reader2 = new NetworkReader(new byte[0]);
            bool result2 = MessagePacker.UnpackMessage(reader2, out int msgType2);
            Assert.That(result2, Is.EqualTo(false));
            Assert.That(msgType2, Is.EqualTo(0));
        }
    }
}
