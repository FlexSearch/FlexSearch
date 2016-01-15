/// <reference path="../../../typings/tsd.d.ts" />
/// <reference path="domain.ts" />
/// <reference path="../client/api.d.ts" />
/// <reference path="../../../typings/ui-grid/ui-grid.d.ts" />

/*
 * Contains helper methods for the generated typescript API
 */
module apiHelpers {
    export class SourceRecord {
        SessionId: string;
        SourceId: string;
        SourceRecordId: string;
        SourceContent: string;
        SourceDisplayName: string;
        SourceStatus: string;
        TotalDupes: number;
        Notes: string;
    }

    export class TargetRecord {
        TargetId: string;
        TargetRecordId: string;
        TargetDisplayName: string;
        TrueDuplicate: boolean;
        Quality: string;
        TargetScore: number;
    }

    export class Session {
        Id: string;
        SessionId: string;
        IndexName: string;
        ProfileName: string;
        JobStartTime: Date;
        JobEndTime: Date;
        SelectionQuery: string;
        DisplayFieldName: string;
        RecordsReturned: number;
        RecordsAvailable: number;
        ThreadCount: number;
    }

    export class Duplicate extends SourceRecord {
        Targets: TargetRecord[]
        SourceStatusName: any
        FlexSearchId: string
    }

    function getFirstDocumentFromSearch(sr: API.Client.SearchResultsResponse) {
        return sr.data.recordsReturned > 0 ? sr.data.documents[0] : null;
    }

    function getOrderByDirection(sortDirection: string) {
        return sortDirection && sortDirection.indexOf("esc") > 0 ? API.Client.SearchQuery.OrderByDirectionEnum.Descending : API.Client.SearchQuery.OrderByDirectionEnum.Ascending;
    }

    export function updateDuplicate(duplicate: Duplicate, commonApi: API.Client.CommonApi) {
        var fields: { [key: string]: string } = {
            sessionid: duplicate.SessionId,
            sourcedisplayname: duplicate.SourceDisplayName,
            sourceid: duplicate.SourceId,
            sourcerecordid: duplicate.SourceRecordId,
            totaldupesfound: duplicate.TotalDupes.toString(),
            type: "source",
            notes: duplicate.Notes,
            sourcestatus: duplicate.SourceStatus,
            targetrecords: JSON.stringify(duplicate.Targets)
        };

        return commonApi.updateDocumentHandled({
            fields: fields,
            id: duplicate.FlexSearchId,
            indexName: "duplicates"
        }, "duplicates", duplicate.FlexSearchId);
    }
    
    // Get the duplicate that needs to be displayed
    export function getDuplicateBySourceId(sessionId, sourceId, commonApi: API.Client.CommonApi) {
        return commonApi.postSearchHandled({
            queryString: "type = 'source' and sessionid = '" + sessionId + "' and sourceid = '" + sourceId + "'",
            columns: ["*"],
            indexName: "duplicates"
        }, "duplicates")
            .then(getFirstDocumentFromSearch);
    }

    export function getRecordsByIds(indexName, ids: any[], commonApi: API.Client.CommonApi) {
        var idStr = ids
            .reduce(function(acc, val) { return acc + ",'" + val + "'" }, "")
            .substr(1);
        return commonApi.postSearchHandled({
            queryString: "_id eq [" + idStr + "] {clausetype: 'or'}",
            columns: ["*"],
            indexName: indexName
        }, indexName)
            .then(r => r.data.documents);
    }

    export function getSessionBySessionId(sessionId, commonApi: API.Client.CommonApi) {
        return commonApi.postSearchHandled({
            queryString: "type = 'session' and sessionid = '" + sessionId + "'",
            columns: ["*"],
            indexName: "duplicates"
        }, "duplicates")
            .then(getFirstDocumentFromSearch);
    }

    export function getDuplicatesFromSession(sessionId, count, skip, commonApi: API.Client.CommonApi, sortby?, sortDirection?) {
        return commonApi.postSearchHandled({
            queryString: "type = 'source' and sessionid = '" + sessionId + "'",
            columns: ["*"],
            count: count,
            skip: skip,
            orderBy: sortby,
            orderByDirection: getOrderByDirection(sortDirection),
            indexName: "duplicates"
        }, "duplicates")
            .then(r => r.data);
    }

    export function getSessions(count, skip, commonApi: API.Client.CommonApi, sortby?, sortDirection?) {
        return commonApi.postSearchHandled({
            queryString: "type = 'session'",
            columns: ["*"],
            count: count,
            skip: skip,
            orderBy: sortby,
            orderByDirection: getOrderByDirection(sortDirection),
            indexName: "duplicates"
        }, "duplicates")
            .then(r => r.data);
    }

    export function submitDuplicateDetection(indexName, searchProfile, displayFieldName, selectionQuery, fileName, commonApi: API.Client.CommonApi,
        threadCount?, maxRecordsToScan?, maxDupsToReturn?) {
        return commonApi.duplicateDetectionHandled({
            selectionQuery: selectionQuery,
            displayName: displayFieldName,
            fileName: fileName,
            threadCount: threadCount,
            indexName: indexName,
            profileName: searchProfile,
            maxRecordsToScan: maxRecordsToScan,
            duplicatesCount: maxDupsToReturn
        }, indexName, searchProfile);
    }
}