// Copyright (c) 2011 Tall Ambitions, LLC
// See included LICENSE for details.
namespace Passive.Glimpse
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Web;
    using global::Glimpse.Core.Extensibility;
    using Passive.Diagnostics;
    using System.Linq;

    [GlimpsePlugin(ShouldSetupInInit = true)]
    public class PassivePlugin : IGlimpsePlugin
    {
        private const string StoreToken = "___Passive_Glimpse_Store";
        private static bool initialized;

        public object GetData(HttpApplication application)
        {
            var data = Store.List.Select(metadata =>
                                         new object[]
                                             {
                                                 metadata.Context,
                                                 metadata.Sql,
                                                 metadata.Arguments.Any() ? metadata.Arguments.ToOrdinalDictionary() : null,
                                                 String.Format("{0:n0} ms", metadata.Stopwatch.Elapsed.TotalMilliseconds),
                                             });
            return data.AddHeader("Context", "Sql", "Arguments", "ElapsedTime");
        }

        public void SetupInit(HttpApplication application)
        {
            lock (StoreToken)
            {
                if(!initialized)
                {
                    QueryTrace.QueryBegin += BeginQuery;
                    QueryTrace.QueryEnd += EndQuery;
                    initialized = true;
                }
            }
        }

        private static void EndQuery(object sender, QueryTraceEventArgs e)
        {
            var metadata = Store.Dictionary[e.Token];
            metadata.Stopwatch.Stop();
        }

        private static void BeginQuery(object sender, QueryTraceEventArgs e)
        {
            var store = Store;
            var metadata = MakeMetadata(e);
            store.List.Enqueue(metadata);
            store.Dictionary.TryAdd(e.Token, metadata);
            metadata.Stopwatch.Start();
        }

        private static GlimpsePassiveMetadata MakeMetadata(QueryTraceEventArgs e)
        {
            return new GlimpsePassiveMetadata
                       {
                           Sql = e.Sql,
                           Arguments = e.Arguments.ToArray(),
                           Context = e.Context,
                           Stopwatch = new Stopwatch()
                       };
        }

        public string Name
        {
            get { return "Passive"; }
        }

        private static GlimpsePassiveStore Store
        {
            get
            {
                var items = HttpContext.Current.Items;
                var store = items[StoreToken] as GlimpsePassiveStore;
                if (store == null)
                {
                    items[StoreToken] = store = new GlimpsePassiveStore();
                }

                return store;
            }
        }

        private class GlimpsePassiveStore
        {
            private readonly ConcurrentQueue<GlimpsePassiveMetadata> list = new ConcurrentQueue<GlimpsePassiveMetadata>();
            private readonly ConcurrentDictionary<Guid, GlimpsePassiveMetadata> dictionary = new ConcurrentDictionary<Guid, GlimpsePassiveMetadata>();

            public ConcurrentQueue<GlimpsePassiveMetadata> List
            {
                get { return this.list; }
            }

            public ConcurrentDictionary<Guid, GlimpsePassiveMetadata> Dictionary
            {
                get { return this.dictionary; }
            }
        }

        private class GlimpsePassiveMetadata
        {
            public string Sql { get; set; }
            public object[] Arguments { get; set; }
            public string Context { get; set; }
            public Stopwatch Stopwatch { get; set; }
        }
    }
}