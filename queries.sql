-- redirects to broken websites
SELECT s.SnapshotId, s.Uri, s.HttpStatusCode, s.Status, s.C_Title, ss.C_Title, ss.Uri, ss2.C_Title, ss2.Uri
FROM Snapshots s
         LEFT JOIN main.WebRefs WR on s.SnapshotId = WR.TargetId
         LEFT Join Snapshots as ss on WR.SourceId = ss.SnapshotId AND WR.Type = 'redirect'
         LEFT JOIN WebRefs wr2 on ss.SnapshotId = wr2.TargetId
         LEFT JOIN Snapshots ss2 on wr2.SourceId = ss2.SnapshotId
WHERE s.Status != 1 AND WR.Type = 'redirect'
  AND (s.HttpStatusCode < 200 OR s.HttpStatusCode > 399)
  AND s.ScrapeId IN (SELECT Scrapes.ScrapeId FROM Scrapes ORDER BY Scrapes.TimeStamp DESC LIMIT 1);

-- normal broken websites
SELECT s.SnapshotId, s.Uri, s.HttpStatusCode, s.Status, s.C_Title, WR.LinkText, ss.C_Title, ss.Uri
FROM Snapshots s
         LEFT JOIN main.WebRefs WR on s.SnapshotId = WR.TargetId
         LEFT Join Snapshots as ss on WR.SourceId = ss.SnapshotId
WHERE s.Status != 1
  AND (s.HttpStatusCode < 200 OR s.HttpStatusCode > 399)
  AND s.ScrapeId IN (SELECT Scrapes.ScrapeId FROM Scrapes ORDER BY Scrapes.TimeStamp DESC LIMIT 1);

-- list all broken websites
SELECT s.SnapshotId, s.Uri, s.HttpStatusCode, s.Status, s.C_Title
FROM Snapshots s
WHERE s.Status != 1
  AND (s.HttpStatusCode < 200 OR s.HttpStatusCode > 399)
  AND s.ScrapeId IN (SELECT Scrapes.ScrapeId FROM Scrapes ORDER BY Scrapes.TimeStamp DESC LIMIT 1);

-- list malformed links
SELECT *
FROM WebRefs
WHERE WebRefs.LinkMalformed
  AND WebRefs.SourceId IN (SELECT Snapshots.SnapshotId
                           FROM Snapshots
                           WHERE Snapshots.ScrapeId IN
                                 (SELECT Scrapes.ScrapeId FROM Scrapes ORDER BY Scrapes.TimeStamp DESC LIMIT 1));

