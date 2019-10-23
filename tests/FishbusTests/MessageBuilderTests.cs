﻿using System;
using System.Linq;
using System.Reflection;
using FishbusTests.MessageHandlers;
using Thon.Hotels.FishBus;
using Xunit;

namespace FishbusTests
{
    public class MessageBuilderTests
    {
        [Fact]
        public void MessageWithMessageIdAttributeUseCustomId()
        {
            var messageId = "messageId";
            var messageWithId = new MessageWithMessageId { Id = messageId };

            var value = MessageBuilder.GetMessageId(messageWithId);

            Assert.Equal(messageId, value);
        }

        [Fact]
        public void MessageWithMessageIdAttributeUseCustomIdEvenWhenNull()
        {
            string messageId = null;
            var messageWithId = new MessageWithMessageId();

            var value = MessageBuilder.GetMessageId(messageWithId);

            Assert.Equal(messageId, value);
        }

        [Fact]
        public void MessageWithMoreThanOneMessageIdAttributeThrowExcption()
        {
            var invalidMessage = new MessageWithTooManyMessageIds { Id = "id", AnothterId = "anotherId" };

            Assert.Throws<Exception>(() => MessageBuilder.GetMessageId(invalidMessage));
        }

        [Fact]
        public void MessageWithMoreThanOneMessageIdAttributeThrowExcptionEvenWithIdsSetToNull()
        {
            var invalidMessage = new MessageWithTooManyMessageIds();
            Assert.Throws<Exception>(() => MessageBuilder.GetMessageId(invalidMessage));
        }

        [Fact]
        public void MessageWithoutMessageIdAttributeIdNotSet()
        {
            var messageWithoutMessageId = new MessageWithoutMessageId();

            var value = MessageBuilder.GetMessageId(messageWithoutMessageId);

            Assert.Equal(string.Empty, value);
        }

        [Fact]
        public void MessageWithoutCorrelationIdHasOneAssigned()
        {
            var message = new MessageWithMessageId();
            var msg = MessageBuilder.BuildMessage(message);

            Assert.True(Guid.TryParse(msg.UserProperties["logCorrelationId"] as string, out _));
        }

        [Fact]
        public void ExistingCorrelationIdAreNotModified()
        {
            var expected = "correlationId";
            var messageWithId = new MessageWithMessageId();
            var msg = MessageBuilder.BuildMessage(messageWithId, expected);

            Assert.Equal(expected, msg.UserProperties["logCorrelationId"] as string);
        }

        [Fact]
        public void MessageWithDelaySetsScheduledEnqueueTime()
        {
            var messageWithId = new MessageWithMessageId();
            var msg = MessageBuilder.BuildDelayedMessage(messageWithId, TimeSpan.FromDays(1), string.Empty);

            Assert.True(DateTime.UtcNow.AddHours(23) < msg.ScheduledEnqueueTimeUtc);
        }

        [Fact]
        public void MessageWithLabelAttributeUsesAttribute()
        {
            var messageWithAttribute = new MessageWithLabelAttribute();
            var msg = MessageBuilder.BuildMessage(messageWithAttribute);

            Assert.Equal("A.Custom.Message.Label", msg.Label);
        }

        [Fact]
        public void MessageWithoutLabelAttributeUsesTypeFullName()
        {
            var messageWithoutAttribute = new MessageA();
            var msg = MessageBuilder.BuildMessage(messageWithoutAttribute);

            Assert.Equal(typeof(MessageA).FullName, msg.Label);
        }

        [Fact]
        public void MessageWithTimeToLiveAttributeShouldMakeBuilderSetTimeToLiveAttributeOnBuiltMessage()
        {
            var timeToLive = new TimeSpan(6, 6, 6);
            var messageWithTimeToLiveAttribute = new ValidMessageWithTimeToLiveAttribute
            {
                TimeToLive = timeToLive
            };
            var msg = MessageBuilder.BuildMessage(messageWithTimeToLiveAttribute);

            Assert.Equal(timeToLive, msg.TimeToLive);
        }


        [Fact]
        public void MessageWithMoreThanOneTimeToLiveAttributeShouldMakeGetTimeToLiveMethodThrow()
        {
            var timeToLive = new TimeSpan(6, 6, 6);
            var messageWithToManyTimeToLiveAttributes = new InvalidMessageWithMoreThanOneTimeToLiveAttribute
            {
                TimeToLive = timeToLive,
                TimeToLive2 = timeToLive,
            };
            Assert.Throws<Exception>(() => MessageBuilder.GetTimeToLive(messageWithToManyTimeToLiveAttributes));
        }

        [Fact]
        public void MessageWithTimeToLiveAttributeButWrongTypeShouldMakeGetTimeToLiveMethodReturnNull()
        {
            var messageWithTimeToLiveAttributeAsString = new InvalidMessageWithTimeToLiveAttributeAsString
            {
                TimeToLiveAsString = "2019-01-01"
            };
            var timeToLive = MessageBuilder.GetTimeToLive(messageWithTimeToLiveAttributeAsString);
            Assert.False(timeToLive.HasValue);
        }

        [Fact]
        public void MessageWithNoTimeToLiveAttributeShouldMakeTimeToLiveMethodReturnNull()
        {
            var messageWithNoTimeToLiveAttribute = new MessageWithNoTimeToLiveAttribute();
            var timeToLive = MessageBuilder.GetTimeToLive(messageWithNoTimeToLiveAttribute);
            Assert.False(timeToLive.HasValue);
        }
    }
}
