﻿/*
 * Copyright (c) 2025 ETH Zürich, IT Services
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace SafeExamBrowser.UserInterface.Contracts.Shell.Events
{
	/// <summary>
	/// Event handler fired by an <see cref="IActionCenterActivator"/> to control the visibility of the <see cref="IActionCenter"/>.
	/// </summary>
	public delegate void ActivatorEventHandler();
}
