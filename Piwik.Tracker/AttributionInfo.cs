﻿using System;
using System.Globalization;

namespace Piwik.Tracker
{
    /// <summary>
    /// Represents visit attribution information: referrer URL, referrer timestamp, campaign name and keyword.
    /// Used in the context of goal converions to attribute the right information.
    /// </summary>
    public class AttributionInfo
    {
        /// <summary>
        /// Gets or sets the name of the campaign.
        /// </summary>
        public string CampaignName { get; set; }

        /// <summary>
        /// Gets or sets the campaign keyword.
        /// </summary>
        public string CampaignKeyword { get; set; }

        /// <summary>
        /// Timestamp at which the referrer was set
        /// </summary>
        public DateTimeOffset ReferrerTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the referrer URL.
        /// </summary>
        public string ReferrerUrl { get; set; }

        /// <summary>
        /// Coverts this instance to a string array.
        /// </summary>
        /// <returns></returns>
        public string[] ToArray()
        {
            var infos = new string[4];
            infos[0] = CampaignName;
            infos[1] = CampaignKeyword;
            infos[2] = DateTimeUtils.ConvertToUnixTime(ReferrerTimestamp);
            infos[3] = ReferrerUrl;
            return infos;
        }
    }
}