using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;

namespace FonctionAppCloud
{
    public class FunctionApp
    {
        private readonly ILogger<FunctionApp> _logger;

        // Configuration Azure Service Bus
        private const string ServiceBusConnectionString = "Endpoint=sb://namespacecloud2.servicebus.windows.net/;SharedAccessKeyName=policy;SharedAccessKey=9R5dha1OP6KvpGvsyTdmaBcILnbPxTOEC+ASbDvohbM=;EntityPath=messagequeue";
        private const string QueueName = "messagequeue";

        public FunctionApp(ILogger<FunctionApp> logger)
        {
            _logger = logger;
        }

        [Function(nameof(FunctionApp))]
        public async Task Run([BlobTrigger("images/{name}", Connection = "BlobConnection")] Stream stream, string name)
        {
            using var blobStreamReader = new StreamReader(stream);
            var content = await blobStreamReader.ReadToEndAsync();
            _logger.LogInformation($"C# Blob trigger function Processed blob\n Name: {name} \n Data: {content}");

            // Envoyer un message à la file d'attente Service Bus
            await SendMessageToServiceBusAsync($"Blob Name: {name}, Blob Content: {content}");
        }

        private async Task SendMessageToServiceBusAsync(string messageBody)
        {
            // Créer le client Service Bus
            ServiceBusClient client = new ServiceBusClient(ServiceBusConnectionString);
            ServiceBusSender sender = client.CreateSender(QueueName);

            try
            {
                ServiceBusMessage message = new ServiceBusMessage(messageBody);
                await sender.SendMessageAsync(message);
                _logger.LogInformation("Message envoyé à Azure Service Bus.");
            }
            finally
            {
                await sender.DisposeAsync();
                await client.DisposeAsync();
            }
        }
    }
}
