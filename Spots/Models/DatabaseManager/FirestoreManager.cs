using Plugin.Firebase.Firestore;
using eatMeet.Utilities;

namespace eatMeet.Firestore
{
    public static class FirestoreManager
    {
        private const int MAXIMUM_RETRIES = 3;
        public static async Task<T?> GetDocumentData<T>(string Collection, string DocumentID)
        {
            T? retval = default;
            int retries = 0;

            while (retval == null && retries < MAXIMUM_RETRIES)
            {
                try
                {
                    IDocumentReference docRef = CrossFirebaseFirestore.Current.GetCollection(Collection).GetDocument(DocumentID);
                    IDocumentSnapshot<T> docSnap = await docRef.GetDocumentSnapshotAsync<T>();
                    retval = docSnap.Data;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    retval = default;
                    retries++;
                }
                if(retval == null)
                {
                    retries++;
                }
            }

            return retval;
        }

        public static async Task<string> SetDocumentData(string Collection, object Document, string DocumentID = "")
        {
            IDocumentReference documentReference;
            if (DocumentID.Length > 0)
            {
                documentReference = CrossFirebaseFirestore.Current.GetCollection(Collection).GetDocument(DocumentID);

                await documentReference.SetDataAsync(Document);
            }
            else
            {
                documentReference = await CrossFirebaseFirestore.Current.GetCollection(Collection).AddDocumentAsync(Document);
            }

            return documentReference.Id;
        }

        public static Task DeleteDocument(string Collection, string DocumentID)
        {
            return CrossFirebaseFirestore.Current.GetCollection(Collection).GetDocument(DocumentID).DeleteDocumentAsync();
        }

        public static async Task UpdateSpecificData(string Collection, string DocumentID, string VariableName, object NewData)
        {
            try
            {
                await CrossFirebaseFirestore.Current
                .GetCollection(Collection)
                .GetDocument(DocumentID)
                .UpdateDataAsync((VariableName, NewData))
                .WaitAsync(TimeSpan.FromMilliseconds(150)); // I dont like this, but for some reason Firebase failed to return after updating the value succesfully.
            }
            catch { /* Try catch is necessary, because it throws an exception when hitting the timeout. */ }
        }

        private static async Task<(IQuery query, bool success)> ApplyPaginationAsync<T>(IQuery query, string collection, string? lastItemQueriedID)
        {
            if (lastItemQueriedID == null)
            {
                return (query, true);
            }

            int retries = 0;
            IDocumentSnapshot<T>? documentSnapshot = null;

            while (documentSnapshot == null && retries < MAXIMUM_RETRIES)
            {
                try
                {
                    documentSnapshot = await CrossFirebaseFirestore.Current
                        .GetCollection(collection)
                        .GetDocument(lastItemQueriedID)
                        .GetDocumentSnapshotAsync<T>();

                    if (documentSnapshot.Data == null)
                    {
                        break;
                    }

                    query = query.StartingAfter(documentSnapshot);
                    return (query, true);
                }
                catch (Exception ex)
                {
                    documentSnapshot = null;
                    Console.WriteLine(ex.Message);
                    retries++;
                }
            }

            return (query, retries < MAXIMUM_RETRIES);
        }

        private static async Task<List<T>> ExecuteQueryAsync<T>(IQuery query)
        {
            List<T> results = [];
            IQuerySnapshot<T> querySnapshot = await query.GetDocumentsAsync<T>();

            foreach (var document in querySnapshot.Documents)
            {
                if (document != null)
                {
                    results.Add(document.Data);
                }
            }

            return results;
        }

        public static async Task<List<T>> QueryFiltered<T>(string collection,
            int? maxItems = null,
            string? filters_orderBy = null,
            Dictionary<string, object>? filters_EqualsTo = null,
            Dictionary<string, object>? filters_ArrayContainsSingle = null,
            string? lastItemQueriedID = null)
        {
            int globalRetries = 0;

            while (globalRetries < MAXIMUM_RETRIES)
            {
                try
                {
                    IQuery query = CrossFirebaseFirestore.Current.GetCollection(collection);

                    if (maxItems != null)
                    {
                        query = query.LimitedTo(maxItems.Value);
                    }

                    foreach (var equalsFilter in filters_EqualsTo ?? [])
                    {
                        query = query.WhereEqualsTo(equalsFilter.Key, equalsFilter.Value);
                    }

                    if (filters_ArrayContainsSingle != null)
                    {
                        if (filters_ArrayContainsSingle.Count != 1) // Required condition as there can only be 1 such filter
                        {
                            throw new Exception("Query with multiple ArrayContainsSingle statement attempted.");
                        }

                        foreach (var arrayContainsSingleFilter in filters_ArrayContainsSingle)
                        {
                            query = query.WhereArrayContains(arrayContainsSingleFilter.Key, arrayContainsSingleFilter.Value);
                        }
                    }

                    if (filters_orderBy != null)
                    {
                        query = query.OrderBy(filters_orderBy);
                    }

                    var (paginatedQuery, paginationSuccess) = await ApplyPaginationAsync<T>(query, collection, lastItemQueriedID);
                    if (!paginationSuccess)
                    {
                        throw new Exception("Pagination failed after maximum retries");
                    }

                    return await ExecuteQueryAsync<T>(paginatedQuery);
                }
                catch (Exception ex)
                {
                    globalRetries++;
                    if (globalRetries >= MAXIMUM_RETRIES)
                    {
                        await UserInterface.DisplayPopUp_Regular("Unhandled Error", ex.Message, "Ok");
                    }
                }
            }

            return [];
        }

        public static async Task<List<T>> QueryFiltered<T>(string collection,
            int? maxItems = null,
            string? filters_orderBy = null,
            Dictionary<string, object>? filters_EqualsTo = null,
            Dictionary<string, object[]>? filters_ArrayContainsAny = null,
            string? lastItemQueriedID = null)
        {
            int globalRetries = 0;

            while (globalRetries < MAXIMUM_RETRIES)
            {
                try
                {
                    IQuery query = CrossFirebaseFirestore.Current.GetCollection(collection);

                    if (maxItems != null)
                    {
                        query = query.LimitedTo(maxItems.Value);
                    }

                    foreach (var equalsFilter in filters_EqualsTo ?? [])
                    {
                        query = query.WhereEqualsTo(equalsFilter.Key, equalsFilter.Value);
                    }

                    if (filters_ArrayContainsAny != null)
                    {
                        if (filters_ArrayContainsAny.Count != 1) // Required condition as there can only be 1 such filter
                        {
                            throw new Exception("Query with multiple ArrayContainsAny statement attempted.");
                        }

                        foreach (var arrayContainsAnyFilter in filters_ArrayContainsAny)
                        {
                            query = query.WhereArrayContainsAny(arrayContainsAnyFilter.Key, arrayContainsAnyFilter.Value);
                        }
                    }

                    if (filters_orderBy != null)
                    {
                        query = query.OrderBy(filters_orderBy);
                    }

                    var (paginatedQuery, paginationSuccess) = await ApplyPaginationAsync<T>(query, collection, lastItemQueriedID);
                    if (!paginationSuccess)
                    {
                        throw new Exception("Pagination failed after maximum retries");
                    }

                    return await ExecuteQueryAsync<T>(paginatedQuery);
                }
                catch (Exception ex)
                {
                    globalRetries++;
                    if (globalRetries >= MAXIMUM_RETRIES)
                    {
                        await UserInterface.DisplayPopUp_Regular("Unhandled Error", ex.Message, "Ok");
                    }
                }
            }

            return [];
        }
    }
}
