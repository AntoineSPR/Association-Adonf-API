namespace AssociationAdonfAPI.Utilities
{
    public static class Env
    {
        public static string GetEnv(string key, string defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            return value;
        }

        public static string GetEnv(string key)
        {
            return Environment.GetEnvironmentVariable(key) ?? string.Empty;
        }

        public static string CONNECTION_STRING => GetEnv(nameof(CONNECTION_STRING), "Host=localhost;Port=5432;Database=AssociationAdonfAPI;Username=postgres;Password=admin;");
        public static string SMTP_HOST => GetEnv(nameof(SMTP_HOST), "");
        public static string SMTP_PORT => GetEnv(nameof(SMTP_PORT), "");
        // Identifiant de connexion SMTP Brevo (ex: 96ab74003@smtp-brevo.com) : sert UNIQUEMENT
        // à authentifier la connexion, ce n'est pas une adresse email valide pour le "From".
        public static string SMTP_EMAILFROM => GetEnv(nameof(SMTP_EMAILFROM), "");
        // Adresse d'expédition affichée dans les emails envoyés : doit être un expéditeur
        // validé (ou un domaine authentifié) dans le compte Brevo.
        public static string SMTP_SENDER_ADDRESS => GetEnv(nameof(SMTP_SENDER_ADDRESS), SMTP_EMAILFROM);
        public static string SMTP_PASSWORD => GetEnv(nameof(SMTP_PASSWORD), "");
        public static string API_BACK_URL => GetEnv(nameof(API_BACK_URL), "https://localhost:7168");
        public static string API_FRONT_URL => GetEnv(nameof(API_FRONT_URL), "http://localhost:4321");
        public static string JWT_KEY => GetEnv(nameof(JWT_KEY), "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWUsImlhdCI6MTUxNjIzOTAyMn0.KMUFsIDTnFmyG3nMiGM6H9FNFUROf3wh7SmqJp-QV31");
        public static int ACCESS_TOKEN_VALIDITY_MINUTES
        {
            get
            {
                if (int.TryParse(GetEnv(nameof(ACCESS_TOKEN_VALIDITY_MINUTES)), out var minutes))
                {
                    return minutes;
                }
                return 60;
            }
        }
        public static int REFRESH_TOKEN_VALIDITY_DAYS
        {
            get
            {
                if (int.TryParse(GetEnv(nameof(REFRESH_TOKEN_VALIDITY_DAYS)), out var days))
                {
                    return days;
                }
                return 30;
            }
        }
    }
}
