﻿using System;
using System.Collections.Generic;
using System.ServiceModel.Channels.Http;
using System.Text;
using System.ServiceModel.Channels;
using System.ServiceModel;

namespace System.ServiceModel.Dispatcher
{
	internal class ErrorProcessingHandler : BaseRequestProcessorHandler
	{
		public ErrorProcessingHandler (IChannel channel)
		{
			duplex = channel as IDuplexChannel;
		}

		IDuplexChannel duplex;

		protected override bool ProcessRequest (MessageProcessingContext mrc)
		{
			Exception ex = mrc.ProcessingException;
			DispatchRuntime dispatchRuntime = mrc.OperationContext.EndpointDispatcher.DispatchRuntime;
			
			//invoke all user handlers
			ChannelDispatcher channelDispatcher = dispatchRuntime.ChannelDispatcher;
			foreach (IErrorHandler handler in channelDispatcher.ErrorHandlers)
				if (handler.HandleError (ex))
					break;

			var isExceptionFromReply = HeadersSent(mrc.OperationContext.RequestContext);
			if (! isExceptionFromReply)
			{
				mrc.ReplyMessage = OperationInvokerHandler.BuildExceptionMessage (mrc, ex, dispatchRuntime.ChannelDispatcher.IncludeExceptionDetailInFaults);
				Reply(mrc);
			}

			return false;
		}

		private static bool HeadersSent(RequestContext requestContext)
		{
			var httpRequestContext = requestContext as HttpRequestContext;
			if (httpRequestContext == null)
				return false;

			var httpStandaloneResponseInfo = httpRequestContext.Context.Response as HttpStandaloneResponseInfo;
			if (httpStandaloneResponseInfo != null)
			{
				return httpStandaloneResponseInfo.HeadersSent;
			}

			return false;
		}

		private void Reply(MessageProcessingContext mrc)
		{
			SetMessageProcessingContextOperation(mrc);

			if (mrc.Operation.IsOneWay)
				return;

			if (duplex != null)
				mrc.Reply (duplex, true);
			else
				mrc.Reply (true);
		}

		private static void SetMessageProcessingContextOperation(MessageProcessingContext mrc)
		{
			DispatchRuntime dispatchRuntime = mrc.OperationContext.EndpointDispatcher.DispatchRuntime;
			if (mrc.Operation == null)
			{
				DispatchOperation operation = OperationInvokerHandler.GetOperation(mrc.IncomingMessage, dispatchRuntime);
				mrc.Operation = operation;
			}
		}
	}
}
