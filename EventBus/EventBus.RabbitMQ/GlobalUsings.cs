global using System.Net.Sockets;
global using System.Text;
global using System.Text.Json;

global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;

global using Polly;
global using Polly.Retry;

global using RabbitMQ.Client;
global using RabbitMQ.Client.Events;
global using RabbitMQ.Client.Exceptions;

global using Tsw.EventBus.Common;
global using Tsw.EventBus.Common.Abstractions;
global using Tsw.EventBus.IntegrationEvents;
global using Tsw.EventBus.RabbitMQ.Extensions;
