using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using static System.Net.Mime.MediaTypeNames;

namespace azureBusFonction
{
    public class FunctionBus
    {
        // Connexion � Azure Service Bus
        private const string ServiceBusConnectionString = "Endpoint=sb://namespacecloud2.servicebus.windows.net/;SharedAccessKeyName=policy;SharedAccessKey=9R5dha1OP6KvpGvsyTdmaBcILnbPxTOEC+ASbDvohbM=;EntityPath=messagequeue";
        private const string QueueName = "messagequeue";

        // Connexion � Azure Blob Storage
        private const string BlobConnectionString = "YourBlobConnectionString";
        private const string InputContainerName = "images";
        private const string OutputContainerName = "processed-images";

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Listening for messages...");

            // Initialisation du client et du r�cepteur Service Bus
            ServiceBusClient client = new ServiceBusClient(ServiceBusConnectionString);
            ServiceBusReceiver receiver = client.CreateReceiver(QueueName);

            // Initialisation du client Blob Storage
            BlobServiceClient blobServiceClient = new BlobServiceClient(BlobConnectionString);

            try
            {
                while (true)
                {
                    // Recevoir un message depuis la file d'attente
                    ServiceBusReceivedMessage message = await receiver.ReceiveMessageAsync();

                    if (message != null)
                    {
                        // Lire le nom du fichier depuis le message
                        string fileName = message.Body.ToString();
                        Console.WriteLine($"Message re�u : {fileName}");

                        // T�l�charger, traiter et d�placer le fichier
                        await ProcessFileAsync(blobServiceClient, fileName);

                        // Marquer le message comme trait� (le supprimer de la file d'attente)
                        await receiver.CompleteMessageAsync(message);
                        Console.WriteLine("Message compl�t�.");
                    }
                    else
                    {
                        // Aucun message disponible pour l'instant, attendre 5 secondes
                        Console.WriteLine("Aucun message disponible.");
                        await Task.Delay(5000);
                    }
                }
            }
            finally
            {
                // Nettoyer les ressources
                await receiver.DisposeAsync();
                await client.DisposeAsync();
            }
        }

        private static async Task ProcessFileAsync(BlobServiceClient blobServiceClient, string fileName)
        {
            // R�cup�rer les conteneurs d'entr�e et de sortie
            BlobContainerClient inputContainer = blobServiceClient.GetBlobContainerClient(InputContainerName);
            BlobContainerClient outputContainer = blobServiceClient.GetBlobContainerClient(OutputContainerName);

            // R�f�rence au blob source
            BlobClient inputBlob = inputContainer.GetBlobClient(fileName);

            // V�rifier si le blob existe
            if (!await inputBlob.ExistsAsync())
            {
                Console.WriteLine($"Le fichier {fileName} n'existe pas dans le conteneur {InputContainerName}.");
                return;
            }

            // T�l�charger le fichier dans un MemoryStream
            MemoryStream imageStream = new MemoryStream();
            await inputBlob.DownloadToAsync(imageStream);
            imageStream.Position = 0;

            // Supprimer le fichier original
            await inputBlob.DeleteAsync();
            Console.WriteLine($"Le fichier original {fileName} a �t� supprim� du conteneur {InputContainerName}.");
        }
    }
}
