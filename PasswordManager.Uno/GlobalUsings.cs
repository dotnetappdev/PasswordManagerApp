global using System;
global using System.Collections.Generic;
global using System.Collections.Immutable;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Localization;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Microsoft.Extensions.Configuration;

global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Controls;
global using Microsoft.UI.Xaml.Controls.Primitives;
global using Microsoft.UI.Xaml.Data;
global using Microsoft.UI.Xaml.Input;
global using Microsoft.UI.Xaml.Media;
global using Microsoft.UI.Xaml.Navigation;
global using Microsoft.UI.Xaml.Shapes;

global using PasswordManager.Mobile.Models;
global using PasswordManager.Mobile.Presentation;
global using PasswordManager.Mobile.Services.Endpoints;

global using Uno.Extensions;
global using Uno.Extensions.Navigation;
global using Uno.Resizetizer;

global using ApplicationExecutionState = Windows.ApplicationModel.Activation.ApplicationExecutionState;

[assembly: Uno.Extensions.Reactive.Config.BindableGenerationTool(3)]
