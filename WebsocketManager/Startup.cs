using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/*
 * https://docs.microsoft.com/it-it/aspnet/core/fundamentals/websockets?view=aspnetcore-2.1
 * 
 */
namespace WebsocketManager
{
	public class Startup
	{
		private static Dictionary<Guid, WebSocket> sockets = new Dictionary<Guid, WebSocket>();

		public void ConfigureServices(IServiceCollection services)
		{
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			//Setting the use of websockets
			app.UseWebSockets();

			//Managing options given by the client
			var webSocketOptions = new WebSocketOptions()
			{
				KeepAliveInterval = TimeSpan.FromSeconds(120),
				ReceiveBufferSize = 4 * 1024
			};
			app.UseWebSockets(webSocketOptions);

			//Setting the websocket
			app.Use(async (context, next) =>
			{
				//Managing "/ws" page
				if (context.Request.Path == "/ws")
				{
					//If a websocket is connecting...
					if (context.WebSockets.IsWebSocketRequest)
					{
						//Creating a websocket
						WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
						//Adding to
						Guid current = AddWebSocket(webSocket);
						Console.WriteLine("[{0}] Connected.", current.ToString());

						//Waiting for a message
						await Update(context, webSocket);
					}
					else
					{//Otherwise error message
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
		/// Repeating the packet received from the client
		/// </summary>
		private async Task Update(HttpContext context, WebSocket webSocket)
		{
			var buffer = new byte[1024 * 4];
			//Receiving the first packet
			WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
			while (!result.CloseStatus.HasValue)
			{
				//Getting websocket GUID
				Guid guid = getGuid(webSocket);

				//Printing data on the console
				string data = Encoding.UTF8.GetString(buffer).TrimEnd('\0'); //https://stackoverflow.com/questions/1003275/how-to-convert-utf-8-byte-to-string#comment51241174_1003289
				Console.WriteLine("[{0}] Message: {1}", guid, data);

				//Sending "OK" to the client
				Console.WriteLine("Sending ok to " + guid);
				String response = "ok";
				var encoded = Encoding.UTF8.GetBytes(response);
				var buffer2 = new ArraySegment<Byte>(encoded, 0, encoded.Length);
				await webSocket.SendAsync(buffer2, WebSocketMessageType.Text, true, CancellationToken.None);

				//Sending back to the client
				//await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

				//Receiving the next packet
				result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
			}
			//Closing Socket
			await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);

			//Removing from dictionary
			Guid socketToRemove = getGuid(webSocket);
			RemoveWebSocket(socketToRemove);

			Console.WriteLine("[{0}] Disconnected.", socketToRemove.ToString());
		}

		/// <summary>
		/// Adding a websocket to the dictionary and generating its GUID
		/// </summary>
		/// <param name="socket">WebSocket that we want to add</param>
		/// <returns>GUID of the added WebSocket</returns>
		public Guid AddWebSocket(WebSocket socket)
		{
			var guid = Guid.NewGuid();
			sockets.TryAdd(guid, socket);
			return guid;
		}

		/// <summary>
		/// Get GUID
		/// </summary>
		/// <param name="socket">Socket that we want to identify</param>
		/// <returns>GUID of the given socket</returns>
		public Guid getGuid(WebSocket socket)
		{
			return KeyByValue(sockets, socket); ;
		}

		/// <summary>
		/// Getting GUID by the given value
		/// </summary>
		/// <param name="dict">Dictionary containing the GUID of every WebSocket</param>
		/// <param name="val">WebSocket that we want to identify</param>
		/// <returns>GUID of the given WebSocket</returns>
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

		/// <summary>
		/// Remove the socket from the dictionary
		/// </summary>
		/// <param name="guid"></param>
		/// <returns></returns>
		private static bool RemoveWebSocket(Guid guid)
		{
			return sockets.Remove(guid);
		}
	}
}
