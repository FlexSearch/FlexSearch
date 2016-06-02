/// <reference path="../../../typings/index.d.ts" />
/// <reference path="../client/api.d.ts" />

/*
 * Contains helper methods for the generated typescript API
 */
module apiHelpers {
    export class SourceRecord {
        sessionId: string;
        sourceId: string;
        sourceRecordId: string;
        sourceContent: string;
        sourceDisplayName: string;
        sourceStatus: string;
        totalDupes: number;
        notes: string;
    }

    export class TargetRecord {
        targetId: string;
        targetRecordId: string;
        targetDisplayName: string;
        trueDuplicate: boolean;
        quality: string;
        targetScore: number;
    }

    export class Session {
        id: string;
        sessionId: string;
        indexName: string;
        profileName: string;
        jobStartTime: Date;
        jobEndTime: Date;
        selectionQuery: string;
        displayFieldName: string;
        recordsReturned: number;
        recordsAvailable: number;
        threadCount: number;
    }

    export class Duplicate extends SourceRecord {
        Targets: TargetRecord[]
        SourceStatusName: any
        FlexSearchId: string
    }

    function getFirstDocumentFromSearch(sr: API.Client.SearchResponse) {
        return sr.data.recordsReturned > 0 ? sr.data.documents[0] : null;
    }

    function getOrderByDirection(sortDirection: string) {
        return sortDirection && sortDirection.indexOf("esc") > 0 ? API.Client.SearchQuery.OrderByDirectionEnum.Descending : API.Client.SearchQuery.OrderByDirectionEnum.Ascending;
    }

    export function updateDuplicate(duplicate: Duplicate, commonApi: API.Client.CommonApi) {
        var fields: { [key: string]: string } = {
            sessionid: duplicate.sessionId,
            sourcedisplayname: duplicate.sourceDisplayName,
            sourceid: duplicate.sourceId,
            sourcerecordid: duplicate.sourceRecordId,
            totaldupesfound: duplicate.totalDupes.toString(),
            type: "source",
            notes: duplicate.notes,
            sourcestatus: duplicate.sourceStatus,
            targetrecords: JSON.stringify(duplicate.Targets)
        };

        return commonApi.createOrUpdateDocumentHandled({
            fields: fields,
            id: duplicate.FlexSearchId,
            indexName: "duplicates"
        }, "duplicates", duplicate.FlexSearchId);
    }

    // Get the duplicate that needs to be displayed
    export function getDuplicateBySourceId(sessionId, sourceId, commonApi: API.Client.CommonApi) {
        return commonApi.searchHandled("duplicates", {
            queryString: "allof(type, 'source') and allof(sessionid, '" + sessionId + "') and allof(sourceid, '" + sourceId + "')",
            columns: ["*"],
            indexName: "duplicates"
        })
            .then(getFirstDocumentFromSearch);
    }

    export function getRecordsByIds(indexName, ids: any[], commonApi: API.Client.CommonApi) {
        var idStr = ids
            .reduce(function(acc, val) { return acc + ",'" + val + "'" }, "")
            .substr(1);
        return commonApi.searchHandled(indexName, {
            queryString: "anyof(_id, " + idStr + ")",
            columns: ["*"],
            indexName: indexName
        })
            .then(r => r.data.documents);
    }

    export function getSessionBySessionId(sessionId, commonApi: API.Client.CommonApi) {
        return commonApi.searchHandled("duplicates", {
            queryString: "allof(type, 'session') and allof(sessionid, '" + sessionId + "')",
            columns: ["*"],
            indexName: "duplicates"
        })
            .then(getFirstDocumentFromSearch);
    }

    export function getDuplicatesFromSession(sessionId, count, skip, commonApi: API.Client.CommonApi, sortby?, sortDirection?) {
        return commonApi.searchHandled("duplicates", {
            queryString: "allof(type, 'source') and allof(sessionid, '" + sessionId + "')",
            columns: ["*"],
            count: count,
            skip: skip ? skip : undefined,
            orderBy: sortby,
            orderByDirection: getOrderByDirection(sortDirection),
            indexName: "duplicates"
        })
            .then(r => r.data);
    }

    export function getSessions(count, skip, commonApi: API.Client.CommonApi, sortby?, sortDirection?) {
        return commonApi.searchHandled("duplicates", {
            queryString: "allof(type, 'session')",
            columns: ["*"],
            count: count,
            skip: skip,
            orderBy: sortby,
            orderByDirection: getOrderByDirection(sortDirection),
            indexName: "duplicates"
        })
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
            predefinedQueryName: searchProfile,
            maxRecordsToScan: maxRecordsToScan,
            duplicatesCount: maxDupsToReturn
        }, indexName, searchProfile);
    }
}
