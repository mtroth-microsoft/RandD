// -----------------------------------------------------------------------
// <copyright file="DefaultConnectionFactory.cs" company="Lensgrinder, Ltd.">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------
using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Infrastructure.DataAccess;
using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

/// <summary>
/// TODO: Update summary.
/// </summary>
public class DefaultConnectionFactory : IConnectionFactory
{
    /// <summary>
    /// The provider to use.
    /// </summary>
    private const string Provider = "System.Data.SqlClient";

    /// <summary>
    /// Store for the logins.
    /// </summary>
    private static ConcurrentDictionary<ShardIdentifier, Login> logins = new ConcurrentDictionary<ShardIdentifier, Login>();

    /// <summary>
    /// Get a connection string based on an actual database type.
    /// </summary>
    /// <param name="databaseType">The database type to inspect.</param>
    /// <returns>The correlated connection string.</returns>
    public string GetConnectionString(DatabaseType databaseType)
    {
        if (databaseType.Protocol == StoreProtocol.TSql)
        {
            return this.GetConnectionString(databaseType.Name);
        }
        else if (databaseType.Protocol == StoreProtocol.MySql)
        {
            MySql.Data.MySqlClient.MySqlConnectionStringBuilder builder = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder();
            ShardIdentifier shardId = LookupServer(databaseType.Name);
            Login login = LookupLogin(databaseType.Name, shardId);
            builder.Server = shardId.DataSource;
            builder.Database = shardId.Catalog;
            builder.Port = (uint)shardId.Port;
            builder.UserID = login.UserName;
            builder.Password = login.Password;
            builder.Pooling = false;

            return builder.ConnectionString;
        }

        throw new NotSupportedException();
    }

    /// <summary>
    /// Get a connection string based on a store type and list of segments.
    /// Empty segments list indicates all segments.
    /// </summary>
    /// <param name="databaseType">The store type.</param>
    /// <returns>The correlated connection string.</returns>
    public string GetConnectionString(string databaseType)
    {
        string smm = databaseType;
        ShardIdentifier shardId = new ShardIdentifier("localhost", "master", 0);
        if (ConfigurationManager.AppSettings.AllKeys.Any(p => p == smm) == true)
        {
            string app = ConfigurationManager.AppSettings.Get(smm); // "vmaz-sqp01.cloudapp.net, 6033";
            string[] tokens = app.Split(',');
            int port = 0;
            if (tokens.Count() == 2)
            {
                port = int.Parse(tokens[1].Trim());
            }

            shardId = new ShardIdentifier(tokens[0], LookupDb(smm), port);
        }

        LookupLogin(smm, shardId);
        return this.GetConnectionString(shardId);
    }

    /// <summary>
    /// Get a connection string based on a store type and list of segments.
    /// Empty segments list indicates all segments.
    /// </summary>
    /// <param name="shardId">The shard identifier</param>
    /// <returns>The correlated connection string.</returns>
    public string GetConnectionString(ShardIdentifier shardId)
    {
        Login login;
        if (logins.TryGetValue(shardId, out login) == false)
        {
            login = new Login() { UserName = "sa" };
            login.Password = Decrypt(null);
        }

        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
        builder.DataSource = shardId.DataSource;
        builder.InitialCatalog = shardId.Catalog;
        builder.UserID = login.UserName;
        builder.Password = login.Password;
        builder.ApplicationName = "DataAccess";
        if (shardId.Port != 0 && shardId.Port != 1433 && shardId.Port != 1434 && shardId.DataSource.Contains(',') == false)
        {
            builder.DataSource = shardId.DataSource + ", " + shardId.Port;
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// Creates an entity connection.
    /// </summary>
    /// <param name="activeModel">The active model.</param>
    /// <param name="metadataResource">The metadata resource string.</param>
    /// <param name="databaseType">The store type for the connection.</param>
    /// <returns>The entity connection to use.</returns>
    public EntityConnection GetEntityConnection(
        string activeModel,
        string metadataResource,
        string databaseType)
    {
        string connectionString = this.GetConnectionString(databaseType);

        return CreateEntityConnection(connectionString, Provider, activeModel, metadataResource);
    }

    /// <summary>
    /// Gets the Connection String Credentials
    /// This string  only contains the credentials required to connect to database.
    /// This string cannot be used to connect to database because it does not have server name
    /// or database name.
    /// </summary>
    /// <param name="databaseType">The Database Key</param>
    /// <returns>Connection String credentials.</returns>
    public string GetCredentialsOnlyConnectionString(string databaseType)
    {
        string connectionString = this.GetConnectionString(databaseType);
        SqlConnectionStringBuilder sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
        sqlConnectionStringBuilder.Remove("Initial Catalog");
        sqlConnectionStringBuilder.Remove("Data Source");

        return sqlConnectionStringBuilder.ConnectionString;
    }

    /// <summary>
    /// Construct the login for a given store.
    /// </summary>
    /// <param name="smm">The store to inspect.</param>
    /// <param name="shardId">The shard id for the connection.</param>
    /// <returns>The discovered login.</returns>
    private static Login LookupLogin(string smm, ShardIdentifier shardId)
    {
        Login login;
        if (logins.TryGetValue(shardId, out login) == false)
        {
            string storeUsrSetting = smm + "User";
            string storePwdSetting = smm + "Password";
            string storeUsr = null, storePwd = null;
            if (ConfigurationManager.AppSettings.AllKeys.Any(p => p == storeUsrSetting) == true)
            {
                string app = ConfigurationManager.AppSettings.Get(storeUsrSetting);
                storeUsr = app;
            }

            if (ConfigurationManager.AppSettings.AllKeys.Any(p => p == storePwdSetting) == true)
            {
                string app = ConfigurationManager.AppSettings.Get(storePwdSetting);
                storePwd = Decrypt(app) ?? app;
            }

            login = new Login();
            login.UserName = storeUsr ?? "sa";
            login.Password = storePwd ?? Decrypt(null);

            logins.TryAdd(shardId, login);
        }

        return login;
    }

    /// <summary>
    /// Helper to lookup the store name.
    /// </summary>
    /// <param name="smm">The store to inspect.</param>
    /// <returns></returns>
    private static string LookupDb(string smm)
    {
        string storeDbSetting = smm + "Db";
        if (ConfigurationManager.AppSettings.AllKeys.Any(p => p == storeDbSetting) == true)
        {
            string app = ConfigurationManager.AppSettings.Get(storeDbSetting); // "NORTHWND";
            storeDbSetting = app;
        }

        return storeDbSetting;
    }

    /// <summary>
    /// Helper to lookup store.
    /// </summary>
    /// <param name="smm">The store to inspect.</param>
    /// <returns>The shard identifier.</returns>
    private static ShardIdentifier LookupServer(string smm)
    {
        ShardIdentifier shardId = new ShardIdentifier("localhost", "master", 0);
        if (ConfigurationManager.AppSettings.AllKeys.Any(p => p == smm) == true)
        {
            string app = ConfigurationManager.AppSettings.Get(smm); // "vmaz-sqp01.cloudapp.net, 6033";
            string[] tokens = app.Split(',');
            int port = 0;
            if (tokens.Count() == 2)
            {
                port = int.Parse(tokens[1].Trim());
            }

            shardId = new ShardIdentifier(tokens[0], LookupDb(smm), port);
        }

        return shardId;
    }

    /// <summary>
    /// Helper to generate the entity connection.
    /// </summary>
    /// <param name="connectionString">The provider connection string.</param>
    /// <param name="provider">The provider name.</param>
    /// <param name="activeModel">The active model.</param>
    /// <param name="metadataResource">The metadata resource.</param>
    /// <returns></returns>
    private EntityConnection CreateEntityConnection(
        string connectionString, 
        string provider,
        string activeModel,
        string metadataResource)
    {
        SqlConnectionStringBuilder sql = new SqlConnectionStringBuilder(connectionString);
        sql.ApplicationName = activeModel;

        EntityConnectionStringBuilder builder = new EntityConnectionStringBuilder();
        builder.Provider = provider;
        builder.Metadata = metadataResource;
        builder.ProviderConnectionString = sql.ToString();

        string entityConnection = builder.ToString();
        EntityConnection ec = new EntityConnection(entityConnection);

        return ec;
    }

    /// <summary>
    /// Decrypt the password.
    /// </summary>
    /// <param name="cipherText">The cipher to decrypt.</param>
    /// <returns>The decrypted password.</returns>
    private static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText) == true)
        {
            cipherText = ConfigurationManager.AppSettings["SqlPassword"];
        }

        VaultHelper helper = new VaultHelper(cipherText);
        Thread thread = new Thread(helper.Run);
        thread.Start();
        helper.Event.WaitOne();

        return helper.Password;
    }

    /// <summary>
    /// Helper class to get the Vault object.
    /// </summary>
    private class VaultHelper
    {
        /// <summary>
        /// The raw data to be decrypted by the vault key.
        /// </summary>
        private string raw;

        /// <summary>
        /// Initializes a new instance of the VaultHelper class.
        /// </summary>
        public VaultHelper(string raw)
        {
            this.raw = raw;
            this.Event = new AutoResetEvent(false);
        }

        /// <summary>
        /// Gets the password.
        /// </summary>
        public string Password
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the event indicating the key has been received.
        /// </summary>
        public AutoResetEvent Event
        {
            get;
            private set;
        }

        /// <summary>
        /// Run the thread to get the certificate from the vault.
        /// </summary>
        public void Run()
        {
            try
            {
                byte[] buffer = this.GetKeyBundle().GetAwaiter().GetResult().Result;
                this.Password = Encoding.ASCII.GetString(buffer);
            }
            catch (Exception ex)
            {
                IMessageLogger logger = Container.Get<IMessageLogger>();
                logger.FireQosEvent(null, "VaultHelper", TimeSpan.FromSeconds(0), ex);
            }
            finally
            {
                this.Event.Set();
            }
        }

        /// <summary>
        /// Get the KeyBundle.
        /// </summary>
        /// <returns>The key bundle.</returns>
        private async Task<KeyOperationResult> GetKeyBundle()
        {
            KeyVaultClient kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(this.GetToken));
            byte[] cipher = Convert.FromBase64String(this.raw);
            KeyOperationResult result = await kv.DecryptAsync(ConfigurationManager.AppSettings["KeyBundleUri"], "RSA-OAEP", cipher).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Get a token to the web app.
        /// </summary>
        /// <param name="authority">The authority of the token.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="scope">The scope.</param>
        /// <returns>The token.</returns>
        private async Task<string> GetToken(string authority, string resource, string scope)
        {
            AuthenticationContext authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(
                ConfigurationManager.AppSettings["ClientId"],
                ConfigurationManager.AppSettings["ClientSecret"]);
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            return result.AccessToken;
        }
    }

    /// <summary>
    /// Helper class to store Login information.
    /// </summary>
    private class Login
    {
        /// <summary>
        /// Gets or sets the user name.
        /// </summary>
        public string UserName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password
        {
            get;
            set;
        }
    }
}
