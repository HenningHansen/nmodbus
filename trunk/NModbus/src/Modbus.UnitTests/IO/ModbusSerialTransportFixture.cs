using System.IO;
using Modbus.Data;
using Modbus.IO;
using Modbus.Message;
using Modbus.UnitTests.Message;
using Modbus.Utility;
using NUnit.Framework;
using Rhino.Mocks;
using System;

namespace Modbus.UnitTests.IO
{
	[TestFixture]
	public class ModbusSerialTransportFixture : ModbusMessageFixture
	{
		[Test]
		public void CreateResponse()
		{
			ModbusAsciiTransport transport = new ModbusAsciiTransport();
			ReadCoilsInputsResponse expectedResponse = new ReadCoilsInputsResponse(Modbus.ReadCoils, 2, 1, new DiscreteCollection(true, false, false, false, false, false, false, true));
			byte lrc = ModbusUtility.CalculateLrc(expectedResponse.MessageFrame);
			ReadCoilsInputsResponse response = transport.CreateResponse<ReadCoilsInputsResponse>(new byte[] { 2, Modbus.ReadCoils, 1, 129, lrc }) as ReadCoilsInputsResponse;
			Assert.IsNotNull(response);
			AssertModbusMessagePropertiesAreEqual(expectedResponse, response);
		}

		[Test, ExpectedException(typeof(IOException))]
		public void CreateResponseErroneousLrc()
		{
			ModbusAsciiTransport transport = new ModbusAsciiTransport();
			transport.CheckFrame = true;
			transport.CreateResponse<ReadCoilsInputsResponse>(new byte[] { 19, Modbus.ReadCoils, 0, 0, 0, 2, 115 });
		}

		[Test]
		public void CreateResponseErroneousLrcDoNotCheckFrame()
		{
			ModbusAsciiTransport transport = new ModbusAsciiTransport();
			transport.CheckFrame = false;
			transport.CreateResponse<ReadCoilsInputsResponse>(new byte[] { 19, Modbus.ReadCoils, 0, 0, 0, 2, 115 });
		}

		/// <summary>
		/// When using the serial RTU protocol the beginning of the message could get mangled leading to an unsupported message type.
		/// We want to be sure to try the message again so clear the RX buffer and try again.
		/// </summary>
		[Test]
		public void UnicastMessage_PurgeReceiveBuffer()
		{
			MockRepository mocks = new MockRepository();
			ISerialResource serialResource = mocks.CreateMock<ISerialResource>();
			ModbusSerialTransport transport = new ModbusRtuTransport(serialResource);

			//transport.Write(null);
			serialResource.DiscardInBuffer();
			serialResource.Write(null, 0, 0);
			LastCall.IgnoreArguments();

			// mangled response
			Expect.Call(serialResource.Read(new byte[] { 0, 0, 0, 0 }, 0, 4)).Return(4);

			//transport.Write(null);
			serialResource.DiscardInBuffer();
			serialResource.Write(null, 0, 0);
			LastCall.IgnoreArguments();

			// read 4 coils from slave id 2
			//Expect.Call(transport.ReadResponse<ReadCoilsInputsResponse>())
			//    .Return(new ReadCoilsInputsResponse(Modbus.ReadCoils, 2, 1, new DiscreteCollection(true, false, true, false, false, false, false, false)));

			// mangled response
			Expect.Call(serialResource.Read(new byte[] { 0, 0, 0, 0 }, 0, 4)).Return(4);

			mocks.ReplayAll();

			ReadCoilsInputsRequest request = new ReadCoilsInputsRequest(Modbus.ReadCoils, 2, 3, 4);
			ReadCoilsInputsResponse expectedResponse = new ReadCoilsInputsResponse(Modbus.ReadCoils, 2, 1, new DiscreteCollection(true, false, true, false, false, false, false, false));
			ReadCoilsInputsResponse response = transport.UnicastMessage<ReadCoilsInputsResponse>(request);
			//Assert.AreEqual(expectedResponse.MessageFrame, response.MessageFrame);

			mocks.VerifyAll();
		}
	}
}
