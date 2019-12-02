using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public class DisplayUser
    {
        [Description("user label")]
        public string name { get; set; }
        public string id { get; set; }
        [Description("最愛的功能")]
        public string favoriteUrl { get; set; }
        [Description("最愛使用次數")]
        public int nb_visits { get; set; }
        [Description("地區")]
        public string continent { get; set; }
        [Description("國家")]
        public string country { get; set; }
        [Description("次數")]
        public int[] usageNumberArray { get; set; }        
    }

    /// <summary>
    /// Class of a action from single WC8 user.
    /// </summary>
    public class DisplayUserAction
    {
        public bool IsError { get; set; }
        public string Version { get; set; }
        [Description("模組")]
        public string Module { get; set; }
        [Description("事件")]
        public string url { get; set; }
        [Description("時間")]
        public DateTime time { get; set; }
    }

    /// <summary>
    /// View model to show on UI listview
    /// </summary>
    public class DisplayRegion
    {
        [Description("地區")]
        public string region { get; set; }
        [Description("最愛的功能")]
        public string favoriteUrl { get; set; }
        [Description("最愛統計次數")]
        public int nb_visits { get; set; }
    }

    /// <summary>
    /// View model to show on UI listview
    /// </summary>
    public class DisplayCountry
    {
        [Description("國家")]
        public string country { get; set; }
        [Description("最愛的功能")]
        public string favoriteUrl { get; set; }
        [Description("最愛統計次數")]
        public int nb_visits { get; set; }
    }

    public enum ReportDuration
    {
        day = 0,
        week,
        month,
        year,
        range
    }

    public enum Platform
    {
        All,
        Android,
        IOS,
        Mac,
        Win
    }

    public enum Projects
    {
        WPSX = 1,
        WCT = 2,
        WDUSB = 4,
        WC8 = 6,
        //WC8_Ad_Verify = 0,
        //View_WC8_Error = -1
    }


    #region objects for json parsing

    public class MatomoUser
    {
        public string idvisitor;
        public string label;
        public int nb_visits;
        public int nb_visits_converted;
        public int nb_actions;
        public int max_actions;
        public int sum_visit_length;
        public int bounce_count;
        public int sum_daily_nb_uniq_visitors;
        public int sum_daily_nb_users;
    }

    public class VisitorProfie
    {
        public string visitorId;
        public string userId;
        public int totalVisits;
        public int totalActions;
        public int totalPageViews;
        public int totalUniquePageViews;
        public List<Page> visitedPages;
        public List<VisitRecord> lastVisits;
        public List<Country> countries;
        public List<Continent> continents;
    }

    public class VisitRecord
    {
        public int idSite;
        public string idVisit;
        public string visitIp;
        public string visitorId;
        public string country;
        public string operatingSystem;
        public string language;
        public string resolution;
        public long firstActionTimestamp;
        public List<ActionDetail> actionDetails;
    }

    public class ActionDetail
    {
        public string type;
        public string url;
        public string pageTitle;
        public string pageIdAction;
        public string idpageview;
        public string pageId;
        public string timeSpent;
        public long timestamp;
        public string eventCategory;
        public string eventAction;
        public int eventValue;
    }

    public class Page
    {
        public string url;
        public int count;
    }

    public class Country
    {
        public string country;
        public string prettyName;
        public int nb_visits;
    }

    public class Continent
    {
        public string continent;
        public string prettyName;
        public int nb_visits;
    }

    #endregion
}
