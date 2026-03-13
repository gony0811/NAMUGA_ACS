/*
 * Copyright (C) 2011 ACS.Communication.Http.uHttpSharp project - http://github.com/raistlinthewiz/uhttpsharp
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.

 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.

 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ACS.Communication.Http.uHttpSharp.Listeners;
using ACS.Communication.Http.uHttpSharp.RequestProviders;
//using ACS.Communication.Http.uHttpSharp.Logging;
using ACS.Framework.Logging;

namespace ACS.Communication.Http.uHttpSharp
{
    public sealed class HttpServer : IDisposable
    {
        //private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        public Logger logger = Logger.GetLogger(typeof(HttpServer));

        private bool _isActive;

        private readonly IList<IHttpRequestHandler> _handlers = new List<IHttpRequestHandler>();
        private readonly IList<IHttpListener> _listeners = new List<IHttpListener>();
        private readonly IHttpRequestProvider _requestProvider;


        public HttpServer(IHttpRequestProvider requestProvider)
        {
            _requestProvider = requestProvider;
        }

        public void Use(IHttpRequestHandler handler)
        {
            _handlers.Add(handler);
        }

        public void Use(IHttpListener listener)
        {
            _listeners.Add(listener);
        }

        public void Start()
        {
            _isActive = true;

            foreach (var listener in _listeners)
            {
                IHttpListener tempListener = listener;

                Task.Factory.StartNew(() => Listen(tempListener));
            }

            logger.Info("Http Server started.");
        }

        private async void Listen(IHttpListener listener)
        {
            var aggregatedHandler = _handlers.Aggregate();      // Handler Aggregate..

            while (_isActive)
            {
                try
                {
                    new HttpClientHandler(await listener.GetClient().ConfigureAwait(false), aggregatedHandler, _requestProvider);
                }
                catch (Exception e)
                {
                    logger.Error("Error while getting client", e);
                }
            }

            logger.Info("Http Server stopped.");
        }

        public void Dispose()
        {
            _isActive = false;
        }
    }
}