using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebsocketManager
{
	public class Startup
	{
		private Dictionary<Guid, WebSocket> sockets = new Dictionary<Guid, WebSocket>();

		public void ConfigureServices(IServiceCollection services)
		{
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			//Imposto l'utilizzo di websocket
			app.UseWebSockets();

			//Gestisco il caso in cui mi mandino delle opzioni
			var webSocketOptions = new WebSocketOptions()
			{
				KeepAliveInterval = TimeSpan.FromSeconds(120),
				ReceiveBufferSize = 4 * 1024
			};
			app.UseWebSockets(webSocketOptions);

			//Setto il websocket
			app.Use(async (context, next) =>
			{
				//Gestisco la pagina "/ws"
				if (context.Request.Path == "/ws")
				{
					//Se si sta connettendo un websocket...
					if (context.WebSockets.IsWebSocketRequest)
					{
						//Creo il websocket
						WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
						Guid current = AddWebSocket(webSocket);
						Console.WriteLine("Socket Connesso " + current.ToString());

						//Aspetto che mi mandino un messaggio
						await Echo(context, webSocket);
					}
					else
					{//Altrimenti errore
						context.Response.StatusCode = 400;
					}
				}
				else
				{
					await next();
				}
			});
			app.UseFileServer();
		}

		/// <summary>
		/// Echo
		/// </summary>
		private async Task Echo(HttpContext context, WebSocket webSocket)
		{
			var buffer = new byte[1024 * 4];
			WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
			while (!result.CloseStatus.HasValue)
			{
				await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

				string data = Encoding.UTF8.GetString(buffer).TrimEnd('\0'); //https://stackoverflow.com/questions/1003275/how-to-convert-utf-8-byte-to-string#comment51241174_1003289
				Console.WriteLine("Messaggio ricevuto: " + data + ".");

				result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
			}
			//Chiusura Socket
			await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
			
			//Todo: rimuovi da dict

			Console.WriteLine("Socket Disconnesso " + getGuid(webSocket).ToString());
		}


		public Guid AddWebSocket(WebSocket socket)
		{
			var guid = Guid.NewGuid();
			sockets.TryAdd(guid, socket);
			return guid;
		}

		public Guid getGuid(WebSocket socket)
		{
			return KeyByValue(sockets, socket); ;
		}

		public static Guid KeyByValue(Dictionary<Guid, WebSocket> dict, WebSocket val)
		{
			Guid key = new Guid();
			foreach (KeyValuePair<Guid, WebSocket>  pair in dict)
			{
				if (pair.Value == val)
				{
					key = pair.Key;
					break;
				}
			}
			return key;
		}

	}
}