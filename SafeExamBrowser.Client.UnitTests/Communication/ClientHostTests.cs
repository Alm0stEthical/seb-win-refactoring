﻿/*
 * Copyright (c) 2018 ETH Zürich, Educational Development and Technology (LET)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SafeExamBrowser.Client.Communication;
using SafeExamBrowser.Contracts.Communication;
using SafeExamBrowser.Contracts.Communication.Data;
using SafeExamBrowser.Contracts.Communication.Hosts;
using SafeExamBrowser.Contracts.Configuration;
using SafeExamBrowser.Contracts.Logging;

namespace SafeExamBrowser.Client.UnitTests.Communication
{
	[TestClass]
	public class ClientHostTests
	{
		private const int PROCESS_ID = 1234;

		private Mock<IConfigurationRepository> configuration;
		private Mock<IHostObject> hostObject;
		private Mock<IHostObjectFactory> hostObjectFactory;
		private Mock<ILogger> logger;
		private ClientHost sut;

		[TestInitialize]
		public void Initialize()
		{
			configuration = new Mock<IConfigurationRepository>();
			hostObject = new Mock<IHostObject>();
			hostObjectFactory = new Mock<IHostObjectFactory>();
			logger = new Mock<ILogger>();

			hostObjectFactory.Setup(f => f.CreateObject(It.IsAny<string>(), It.IsAny<ICommunication>())).Returns(hostObject.Object);

			sut = new ClientHost("net:pipe://some/address", hostObjectFactory.Object, logger.Object, PROCESS_ID);
		}

		[TestMethod]
		public void MustOnlyAllowConnectionIfTokenIsValid()
		{
			var token = Guid.NewGuid();

			sut.StartupToken = token;

			var response = sut.Connect(token);

			Assert.IsNotNull(response);
			Assert.IsTrue(response.ConnectionEstablished);
		}

		[TestMethod]
		public void MustOnlyAllowOneConcurrentConnection()
		{
			var token = Guid.NewGuid();

			sut.StartupToken = token;

			var response1 = sut.Connect(token);
			var response2 = sut.Connect(token);
			var response3 = sut.Connect(token);

			Assert.IsNotNull(response1);
			Assert.IsNotNull(response2);
			Assert.IsNotNull(response3);
			Assert.IsNotNull(response1.CommunicationToken);
			Assert.IsNull(response2.CommunicationToken);
			Assert.IsNull(response3.CommunicationToken);
			Assert.IsTrue(response1.ConnectionEstablished);
			Assert.IsFalse(response2.ConnectionEstablished);
			Assert.IsFalse(response3.ConnectionEstablished);
		}

		[TestMethod]
		public void MustCorrectlyDisconnect()
		{
			var token = Guid.NewGuid();

			sut.StartupToken = token;

			var connectionResponse = sut.Connect(token);
			var response = sut.Disconnect(new DisconnectionMessage { CommunicationToken = connectionResponse.CommunicationToken.Value });

			Assert.IsNotNull(response);
			Assert.IsTrue(response.ConnectionTerminated);
		}

		[TestMethod]
		public void MustNotAllowReconnectionAfterDisconnection()
		{
			var token = sut.StartupToken = Guid.NewGuid();
			var response = sut.Connect(token);

			sut.Disconnect(new DisconnectionMessage { CommunicationToken = response.CommunicationToken.Value });
			sut.StartupToken = token = Guid.NewGuid();

			response = sut.Connect(token);

			Assert.IsFalse(response.ConnectionEstablished);
		}

		[TestMethod]
		public void MustHandleAuthenticationRequestCorrectly()
		{
			sut.StartupToken = Guid.Empty;

			var token = sut.Connect(Guid.Empty).CommunicationToken.Value;
			var message = new SimpleMessage(SimpleMessagePurport.Authenticate) { CommunicationToken = token };
			var response = sut.Send(message);

			Assert.IsNotNull(response);
			Assert.IsInstanceOfType(response, typeof(AuthenticationResponse));
			Assert.AreEqual(PROCESS_ID, (response as AuthenticationResponse)?.ProcessId);
		}

		[TestMethod]
		public void MustHandleShutdownRequestCorrectly()
		{
			var shutdownRequested = false;

			sut.Shutdown += () => shutdownRequested = true;
			sut.StartupToken = Guid.Empty;

			var token = sut.Connect(Guid.Empty).CommunicationToken.Value;
			var message = new SimpleMessage(SimpleMessagePurport.Shutdown) { CommunicationToken = token };
			var response = sut.Send(message);

			Assert.IsTrue(shutdownRequested);
			Assert.IsNotNull(response);
			Assert.IsInstanceOfType(response, typeof(SimpleResponse));
			Assert.AreEqual(SimpleResponsePurport.Acknowledged, (response as SimpleResponse)?.Purport);
		}

		[TestMethod]
		public void MustReturnUnknownMessageAsDefault()
		{
			sut.StartupToken = Guid.Empty;

			var token = sut.Connect(Guid.Empty).CommunicationToken.Value;
			var message = new TestMessage { CommunicationToken = token } as Message;
			var response = sut.Send(message);

			Assert.IsNotNull(response);
			Assert.IsInstanceOfType(response, typeof(SimpleResponse));
			Assert.AreEqual(SimpleResponsePurport.UnknownMessage, (response as SimpleResponse)?.Purport);

			message = new SimpleMessage(default(SimpleMessagePurport)) { CommunicationToken = token };
			response = sut.Send(message);

			Assert.IsNotNull(response);
			Assert.IsInstanceOfType(response, typeof(SimpleResponse));
			Assert.AreEqual(SimpleResponsePurport.UnknownMessage, (response as SimpleResponse)?.Purport);
		}

		private class TestMessage : Message { };
	}
}