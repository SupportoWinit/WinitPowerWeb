using System.Configuration;
using System.Diagnostics;
using System.Web.Configuration;
using System.Xml;

namespace Business
{
  public class PowerWebConfig : IConfigurationSectionHandler
  {
    #region Fields

    private static string _connectionString = "";
    private static bool _initialized;
    private readonly object _lockObj = new object();

    #endregion

    #region Methods

    public object Create(object parent, object configContext, XmlNode section)
    {
      XmlNode sqlServerNode = section.SelectSingleNode("SqlServer");
      lock (_lockObj)
      {
        if (sqlServerNode != null)
        {
          XmlAttribute attribute = sqlServerNode.Attributes["ConnectionStringName"];
          Debug.Assert(attribute != null);
          if ((attribute != null) && (WebConfigurationManager.ConnectionStrings[attribute.Value] != null))
            _connectionString = WebConfigurationManager.ConnectionStrings[attribute.Value].ConnectionString;
        }
      }

      return null;
    }

    /// <summary>
    /// Initializes the NopConfig object
    /// </summary>
    public static void Init()
    {
      if (!_initialized)
      {
        ConfigurationManager.GetSection("PowerWebConfig");
        _initialized = true;
      }
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the connection string that is used to connect to the storage
    /// </summary>
    public static string ConnectionString
    {
      get
      {
        return _connectionString;
      }
      set
      {
        _connectionString = value;
      }
    }

    /// <summary>
    /// Gets a value indicating whether [connection string is set].
    /// </summary>
    /// <value>
    /// 	<c>true</c> if [connection string is set]; otherwise, <c>false</c>.
    /// </value>
    public static bool IsConnectionStringSet
    {
      get { return !string.IsNullOrEmpty(ConnectionString); }
    }

    public static double CookieExpires
    {
      get { return 24; }
    }

    #endregion
  }
}
