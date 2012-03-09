// Copyright 2007-2011 The Apache Software Foundation.
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
namespace Topshelf
{
    using System;
    using HostConfigurators;
	using Magnum.CommandLineParser;
	using Magnum.Extensions;
	using Magnum.Monads.Parser;
	using Options;


	public static class CommandLineConfigurator
	{
		public static void ApplyCommandLine(this HostConfigurator configurator)
		{
			CommandLine.Parse<Option>(InitializeCommandLineParser)
				.Each(option => option.ApplyTo(configurator));
		}

		public static void ApplyCommandLine(this HostConfigurator configurator, string commandLine)
		{
			CommandLine.Parse<Option>(commandLine, InitializeCommandLineParser)
				.Each(option => option.ApplyTo(configurator));
		}

		static void InitializeCommandLineParser(ICommandLineElementParser<Option> x)
		{
			x.Add((from arg in x.Argument("install")
			       select (Option)new InstallOption())
			      	.Or(from arg in x.Argument("uninstall")
			      	    select (Option)new UninstallOption())
			      	.Or(from arg in x.Argument("start")
			      	    select (Option)new StartOption())
			      	.Or(from arg in x.Argument("help")
			      	    select (Option)new HelpOption())
			      	.Or(from arg in x.Argument("stop")
			      	    select (Option)new StopOption())
			      	.Or(from arg in x.Switch("sudo")
			      	    select (Option)new SudoOption())
			      	.Or(from arg in x.Argument("run")
			      	    select (Option)new RunOption())
			      	.Or(from username in x.Definition("username")
			      	    from password in x.Definition("password")
			      	    select (Option)new ServiceAccountOption(username.Value, password.Value))
			      	.Or(from autostart in x.Switch("autostart")
			      	    select (Option)new AutostartOption())
                    .Or(from interactive in x.Switch("interactive")
                        select (Option)new InteractiveOption())
			      	.Or(from autostart in x.Switch("localservice")
			      	    select (Option)new LocalServiceOption())
			      	.Or(from autostart in x.Switch("networkservice")
			      	    select (Option)new NetworkServiceOption())
			      	.Or(from autostart in x.Switch("help")
			      	    select (Option)new HelpOption())
                    .Or(from svcname in x.Definition("servicename")
                        select (Option)new ServiceNameOption(svcname.Value))
                    .Or(from desc in x.Definition("description")
                        select (Option)new ServiceDescriptionOption(desc.Value))
                    .Or(from disp in x.Definition("displayname")
                        select (Option)new DisplayNameOption(disp.Value))
			      	.Or(from instance in x.Definition("instance")
			      	    select (Option)new InstanceOption(instance.Value))
                    .Or(from arg in x.Argument("shelf")
                        from uri in x.Definition("uri")
                        from pipe in x.Definition("pipe")
                        from bootstrapper in x.Definition("bootstrapper")
                        select (Option)new ShelfOption(uri.Value, pipe.Value, bootstrapper.Value)));
		}
	}
}