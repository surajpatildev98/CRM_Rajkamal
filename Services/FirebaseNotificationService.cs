using CRMWebServices.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace CRMWebServices.Services
{
    public class FirebaseNotificationService: IFirebaseNotificationService
    {
        private readonly ILogger<FirebaseNotificationService> _logger;

        public FirebaseNotificationService(ILogger<FirebaseNotificationService> logger)
        {
            _logger = logger;
            InitializeFirebase();
        }

        private void InitializeFirebase()
        {
            if (FirebaseApp.DefaultInstance != null)
                return;

            var credentialPath = Path.Combine(Directory.GetCurrentDirectory(),"Firebase","firebase-service-account.json");

            if (!File.Exists(credentialPath))
            {
                throw new FileNotFoundException("Firebase service account file was not found.",credentialPath);
            }

            FirebaseApp.Create(new AppOptions
            {
                Credential =GoogleCredential.FromFile(credentialPath)
            });

            _logger.LogInformation("Firebase Admin SDK initialized successfully.");
        }

        public async Task<FirebaseSendResult> SendNotificationAsync(string deviceToken,string title,string message,Dictionary<string, string>? data = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceToken))
                {
                    return new FirebaseSendResult
                    {
                        Success = false,
                        ErrorMessage = "Firebase device token is empty."
                    };
                }

                var notification = new Notification
                {
                    Title = title,
                    Body = message
                };

                var firebaseMessage = new Message
                {
                    Token = deviceToken,
                    Notification = notification,
                    Data = data
                };

                var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(firebaseMessage);

                return new FirebaseSendResult
                {
                    Success = true,
                    MessageId = messageId
                };
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError(ex,"Firebase notification failed.");

                return new FirebaseSendResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Unexpected Firebase notification error.");

                return new FirebaseSendResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}