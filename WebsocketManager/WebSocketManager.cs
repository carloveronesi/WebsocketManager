using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebsocketManager
{
	public class WebSocketManager
	{
		private readonly ConcurrentDictionary<Guid, WebSocket> _sockets;

		protected WebSocketManager()
		{
			_sockets = new ConcurrentDictionary<Guid, WebSocket>();
		}

		public Guid AddWebSocket(WebSocket socket)
		{
			var guid = Guid.NewGuid();
			_sockets.TryAdd(guid, socket);

			return guid;
		}

		public async Task SendAsync(Guid guid, string message)
		{
			if (!_sockets.TryGetValue(guid, out var socket))
				return;

			await SendMessageAsync(socket, message);
		}

		public async Task SendAllAsync(string message)
		{
			foreach (var socket in _sockets)
			{
				if (socket.Value.State == WebSocketState.Open)
					await SendMessageAsync(socket.Value, message);
				else
					await RemoveWebSocketAsync(socket.Key);
			}
		}

		private async Task SendMessageAsync(WebSocket socket, string message)
		{
			await socket.SendAsync(
				new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)),
				WebSocketMessageType.Text,
				true,
				CancellationToken.None);
		}

		private async Task RemoveWebSocketAsync(Guid guid)
		{
			if (!_sockets.TryRemove(guid, out var socket))
				return;

			if (socket?.State == WebSocketState.Open)
				await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);

			socket.Dispose();
		}
	}
}
