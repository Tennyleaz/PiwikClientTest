using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiwikClientTest
{
    /// <summary>
    /// View model to show on UI listview
    /// </summary>
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
    }

    /// <summary>
    /// View model to show on UI listview
    /// </summary>
    public class DisplayRegion
    {
        public string region { get; set; }
        public string favoriteUrl { get; set; }
        public int nb_visits { get; set; }
    }

    /// <summary>
    /// View model to show on UI listview
    /// </summary>
    public class DisplayCountry
    {
        public string country { get; set; }
        public string favoriteUrl { get; set; }
        public int nb_visits { get; set; }
    }

    public enum ReportDuration
    {
        day=0,
        week,
        month,
        year,
        range
    }

    #region objects for json parsing

    class MatomoUser
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

    class VisitorProfie
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

    class VisitRecord
    {
        public int idSite;
        public string idVisit;
        public string visitIp;
        public string visitorId;
        public List<ActionDetail> actionDetails;
    }

    class ActionDetail
    {
        public string type;
        public string url;
        public string pageTitle;
        public string pageIdAction;
        public string idpageview;
        public string pageId;
        public string timeSpent;
        public long timestamp;
    }

    class Page
    {
        public string url;
        public int count;
    }

    class Country
    {        
        public string country;
        public string prettyName;
        public int nb_visits;
    }

    class Continent
    {
        public string continent;
        public string prettyName;
        public int nb_visits;
    }

    #endregion
}
