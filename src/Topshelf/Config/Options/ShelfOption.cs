﻿// Copyright 2007-2011 The Apache Software Foundation.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace Topshelf.Options
{
	using System;
	using Builders;
	using HostConfigurators;
	using Internal;


	public class ShelfOption :
		Option
	{
		readonly string _bootstrapper;
		readonly string _pipe;
		readonly Uri _uri;

		public ShelfOption([NotNull] string uri, [NotNull] string pipe, [NotNull] string bootstrapper)
		{
			_uri = new Uri(uri);
			_pipe = pipe;
			_bootstrapper = bootstrapper;
		}

		public void ApplyTo([NotNull] HostConfigurator configurator)
		{
			if (configurator == null)
				throw new ArgumentNullException("configurator");

			configurator.UseBuilder(description => new ProcessShelfBuilder(description, _uri, _pipe, _bootstrapper));
		}
	}
}