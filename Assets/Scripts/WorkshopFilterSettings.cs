using Newtonsoft.Json.Converters;
using Steamworks;
using Steamworks.Ugc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class WorkshopFilterSettings : MonoBehaviour
{

    public static UgcType CurrentFilter = UgcType.Items;

    public static UgcOwnershipType ugcOwnershipType = UgcOwnershipType.Discover;

    public static UgcSortOrder ugcSortOrder = UgcSortOrder.Recent;

    public static Query GetUGCQuery()
    {
        Query query = new Query(CurrentFilter);
        switch (ugcOwnershipType)
        {
            case UgcOwnershipType.Subscribed:
                query = query.WhereUserSubscribed(SteamClient.SteamId);
                break;
            case UgcOwnershipType.Published:
                query = query.WhereUserPublished(SteamClient.SteamId);
                break;
        }

        switch (ugcSortOrder)
        {
            case UgcSortOrder.Popular:
                query = query.RankedByTotalUniqueSubscriptions();
                break;
            case UgcSortOrder.Recent:
                query = query.SortByCreationDate();
                break;
            case UgcSortOrder.Updated:
                query = query.SortByUpdateDate();
                break;
        }

        return query;
    }

    // Source - https://stackoverflow.com/a
    // Posted by Hans Passant, modified by community. See post 'Timeline' for change history
    // Retrieved 2026-01-16, License - CC BY-SA 2.5

    [DllImport("msvcrt.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr wcsstr(string toSearch, string toFind);

}

public enum UgcOwnershipType : int
{
    Discover = 0,       //Visible but not owned
    Subscribed = 1,        // In the user's library
    Published = 2          // Published by the user
}

public enum UgcSortOrder : int
{
    Popular = 0,
    Recent = 1,
    Updated = 2,
}

