﻿/*
 * Copyright (c) 2018 ETH Zürich, Educational Development and Technology (LET)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CefSharp;
using SafeExamBrowser.Browser.Handlers;
using SafeExamBrowser.Contracts.Behaviour;
using SafeExamBrowser.Contracts.Configuration;
using SafeExamBrowser.Contracts.I18n;
using SafeExamBrowser.Contracts.UserInterface;
using SafeExamBrowser.Contracts.UserInterface.Taskbar;
using BrowserSettings = SafeExamBrowser.Contracts.Configuration.Settings.BrowserSettings;

namespace SafeExamBrowser.Browser
{
	public class BrowserApplicationController : IApplicationController
	{
		private IApplicationButton button;
		private IList<IApplicationInstance> instances = new List<IApplicationInstance>();
		private BrowserSettings settings;
		private RuntimeInfo runtimeInfo;
		private IUserInterfaceFactory uiFactory;
		private IText text;

		public BrowserApplicationController(
			BrowserSettings settings,
			RuntimeInfo runtimeInfo,
			IText text,
			IUserInterfaceFactory uiFactory)
		{
			this.runtimeInfo = runtimeInfo;
			this.settings = settings;
			this.text = text;
			this.uiFactory = uiFactory;
		}

		public void Initialize()
		{
			var cefSettings = InitializeCefSettings();
			var success = Cef.Initialize(cefSettings, true, null);

			if (!success)
			{
				throw new Exception("Failed to initialize the browser engine!");
			}
		}

		public void RegisterApplicationButton(IApplicationButton button)
		{
			this.button = button;
			this.button.Clicked += Button_OnClick;
		}

		public void Terminate()
		{
			foreach (var instance in instances)
			{
				instance.Terminated -= Instance_Terminated;
				instance.Window.Close();
			}

			Cef.Shutdown();
		}

		private void CreateNewInstance()
		{
			var instance = new BrowserApplicationInstance(settings, text, uiFactory, instances.Count == 0);

			button.RegisterInstance(instance);
			instances.Add(instance);

			instance.ConfigurationDetected += Instance_ConfigurationDetected;
			instance.Terminated += Instance_Terminated;
			instance.Window.Show();
		}

		private CefSettings InitializeCefSettings()
		{
			var cefSettings = new CefSettings
			{
				CachePath = runtimeInfo.BrowserCachePath,
				LogFile = runtimeInfo.BrowserLogFile,
				// TODO: Set according to current application LogLevel!
				LogSeverity = LogSeverity.Verbose
			};

			cefSettings.RegisterScheme(new CefCustomScheme { SchemeName = "seb", SchemeHandlerFactory = new SchemeHandlerFactory() });
			cefSettings.RegisterScheme(new CefCustomScheme { SchemeName = "sebs", SchemeHandlerFactory = new SchemeHandlerFactory() });

			return cefSettings;
		}

		private void Button_OnClick(Guid? instanceId = null)
		{
			if (instanceId.HasValue)
			{
				instances.FirstOrDefault(i => i.Id == instanceId)?.Window?.BringToForeground();
			}
			else
			{
				CreateNewInstance();
			}
		}

		private void Instance_ConfigurationDetected(string url, CancelEventArgs args)
		{
			// TODO:
			// 1. Ask whether reconfiguration should be attempted
			// 2. Contact runtime and ask whether configuration valid and reconfiguration allowed
			//    - If yes, do nothing and wait for shutdown command
			//    - If no, show message box and NAVIGATE TO PREVIOUS PAGE -> but how?

			var result = uiFactory.Show(TextKey.MessageBox_ReconfigureQuestion, TextKey.MessageBox_ReconfigureQuestionTitle, MessageBoxAction.YesNo, MessageBoxIcon.Question);

			args.Cancel = result == MessageBoxResult.No;
		}

		private void Instance_Terminated(Guid id)
		{
			instances.Remove(instances.FirstOrDefault(i => i.Id == id));
		}
	}
}
