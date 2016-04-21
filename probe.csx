#r ".AnFake/AnFake.Api.v1.dll"
#r ".AnFake/AnFake.Core.dll"

using System;
using System.Linq;
using AnFake.Api;
using AnFake.Core;

Console.WriteLine("I'm FIRST!");

"Probe".AsTarget().Do(() =>
{
	Trace.Info("Hello!");
});