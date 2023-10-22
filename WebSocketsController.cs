using System.Net.WebSockets;
using System.Text;

namespace WebSocket_ASPNet
{
    public class WebSocketsController
    {
        private readonly RequestDelegate _next;

        public WebSocketsController(RequestDelegate next) {
            _next = next;
        }

        public async Task Invoke(HttpContext context) {

            //Si no es una petición de socket no la procesa
            if (!context.WebSockets.IsWebSocketRequest) {
                await _next.Invoke(context);
                return;
            }

            //Validar la petición del socket
            var ct = context.RequestAborted;
            using (var socket = await context.WebSockets.AcceptWebSocketAsync()) {
                var mensaje = await ReceiveStringAsync(socket, ct);
                if (mensaje == null) { return; }

                //Vamos a inventar dos tipos de mensajes:
                // 1. Simples: sólo llega una cadena de texto
                // 2. Compuestos: requerimos parametros (Se separan con #)

                //Simples
                switch (mensaje.ToLower()) {
                    case "hola":
                        await SendStringAsync(socket, "Hola, ¿Cómo estás?, Bienvenido!", ct);
                        break;
                    case "adios":
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Desconectado", ct);
                        break;
                    default:
                        await SendStringAsync(socket, "Lo siento, no se entiende el mensaje", ct);
                        break;
                }

                //Mensaje con Párametros
                if (mensaje.Contains('#')) {
                    string[] mensajeCompuesto = mensaje.ToLower().Split('#');
                    switch (mensajeCompuesto[0]) {
                        case "hola":
                            await SendStringAsync(socket, $"Hola usaurio {mensajeCompuesto[1]}"  , ct);
                            break;
                        case "adios":
                            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Desconectado", ct);
                            break;
                        default:
                            await SendStringAsync(socket, "Lo siento, no se entiende el mensaje", ct);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Recibe los mensajes, codifica y convierte en UTF-8
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static async Task<string> ReceiveStringAsync(WebSocket socket, CancellationToken ct) {
            //Recibe un mensaje que debe ser decodificado
            var buffer = new ArraySegment<byte>(new byte[8192]);
            using (var ms = new MemoryStream()) {
                WebSocketReceiveResult wsResult;
                do {
                    ct.ThrowIfCancellationRequested();

                    wsResult = await socket.ReceiveAsync(buffer, ct);
                    ms.Write(buffer.Array, buffer.Offset, wsResult.Count);

                } while (!wsResult.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);
                if (wsResult.MessageType != WebSocketMessageType.Text) {
                    throw new Exception("Mensaje inesperado!");
                }

                //Codificar como UTF-8 https://tools.ietf.org/html/rfc6455#section-5.6
                using (var reader = new StreamReader(ms, Encoding.UTF8)) { 
                    return await reader.ReadToEndAsync();
                }

            
            }
        }

        /// <summary>
        /// Envía mensajes al cliente
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static Task SendStringAsync(WebSocket socket, string data, CancellationToken ct = default) {
            var buffer = Encoding.UTF8.GetBytes(data);
            var segmet = new ArraySegment<byte>(buffer);
            return socket.SendAsync(segmet, WebSocketMessageType.Text, true, ct); 
        }

        
    }//EnClass
}//EndNamespace
