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

namespace Stuff
{
    using System.IO;
    using Topshelf;
    using log4net.Config;


    class Program
    {
        static void Main(string[] args)
        {
            XmlConfigurator.ConfigureAndWatch(new FileInfo(".\\log4net.config"));
           
            HostFactory.Run(x =>
                {
                    x.UseLog4Net();
                    x.Service<TownCrier>(s =>
                        {
                            s.SetServiceName("TownCrier");
                            s.ConstructUsing(name => new TownCrier());
                            s.WhenStarted(tc => tc.Start());
                            s.WhenStopped(tc => tc.Stop());
                        });

                    x.RunAsLocalSystem();

                    x.SetDescription("Sample Topshelf Host");
                    x.SetDisplayName("Stuff");
                    x.SetServiceName("stuff");
                });
        }
    }
}