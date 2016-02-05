using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lime.Protocol.Listeners;
using Lime.Protocol.Network;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Lime.Protocol.UnitTests.Listeners
{
    [TestFixture]
    public class DataflowChannelListenerTests
    {
        protected Mock<IMessageChannel> _messageChannel;
        protected Mock<INotificationChannel> _notificationChannel;
        protected Mock<ICommandChannel> _commandChannel;

        protected BlockingCollection<Message> _producedMessages;
        protected BlockingCollection<Notification> _producedNotifications;
        protected BlockingCollection<Command> _producedCommands;

        private BufferBlock<Message> _messageBufferBlock;        
        private BufferBlock<Notification> _notificationBufferBlock;
        private BufferBlock<Command> _commandBufferBlock;
        private CancellationTokenSource _cts;

        [SetUp]
        public void Setup()
        {
            _messageChannel = new Mock<IMessageChannel>();
            _notificationChannel = new Mock<INotificationChannel>();
            _commandChannel = new Mock<ICommandChannel>();

            _producedMessages = new BlockingCollection<Message>();
            _producedNotifications = new BlockingCollection<Notification>();
            _producedCommands = new BlockingCollection<Command>();

            _messageChannel
                .Setup(m => m.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken cancellationToken) => _producedMessages.Take(cancellationToken).AsCompletedTask());
            _notificationChannel
                .Setup(m => m.ReceiveNotificationAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken cancellationToken) => _producedNotifications.Take(cancellationToken).AsCompletedTask());
            _commandChannel
                .Setup(m => m.ReceiveCommandAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken cancellationToken) => _producedCommands.Take(cancellationToken).AsCompletedTask());

            _cts = new CancellationTokenSource();
            var options = new DataflowBlockOptions()
            {
                CancellationToken = _cts.Token
            };
            _messageBufferBlock = new BufferBlock<Message>(options);
            _notificationBufferBlock = new BufferBlock<Notification>(options);
            _commandBufferBlock = new BufferBlock<Command>(options);
        }

        [TearDown]
        public void TearDown()
        {
            _cts.Dispose();            
        }

        private DataflowChannelListener GetAndStartTarget()
        {
            var target = new DataflowChannelListener(
                _messageChannel.Object, _notificationChannel.Object, _commandChannel.Object,
                _messageBufferBlock, _notificationBufferBlock, _commandBufferBlock);

            target.Start();
            return target;            
        }

        [Test]
        public async Task Start_MessageReceived_SendsToBuffer()
        {
            // Arrange            
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            var target = GetAndStartTarget();

            // Act
            _producedMessages.Add(message);
            await Task.Delay(250);

            // Assert
            Message actual;
            _messageBufferBlock.TryReceive(out actual).ShouldBeTrue();
            actual.ShouldBe(message);
            target.Stop();            
        }

        [Test]
        public async Task Start_MultipleMessagesReceived_SendsToBuffer()
        {
            // Arrange
            var messages = new List<Message>();
            var count = Dummy.CreateRandomInt(100);
            for (int i = 0; i < count; i++)
            {
                messages.Add(
                    Dummy.CreateMessage(Dummy.CreateTextContent()));
            }
            var target = GetAndStartTarget();

            // Act
            foreach (var message in messages)
            {
                _producedMessages.Add(message);
            }

            await Task.Delay(count * 15);

            // Assert
            _messageBufferBlock.Count.ShouldBe(count);

            Message actual;
            while (_messageBufferBlock.TryReceive(out actual))
            {
                messages.ShouldContain(actual);
            }           
        }

        [Test]
        public async Task Start_CompletedMessageBufferBlock_StopsConsumerTask()
        {
            // Arrange            
            var message = Dummy.CreateMessage(Dummy.CreateTextContent());
            var target = GetAndStartTarget();

            // Act
            _messageBufferBlock.Complete();
            _producedMessages.Add(message);

            // Assert
            await target.MessageListenerTask;
        }
        
        [Test]
        public async Task Start_NotificationReceived_SendsToBuffer()
        {
            // Arrange            
            var notification = Dummy.CreateNotification(Event.Authorized);
            var target = GetAndStartTarget();

            // Act
            _producedNotifications.Add(notification);
            await Task.Delay(250);

            // Assert
            Notification actual;
            _notificationBufferBlock.TryReceive(out actual).ShouldBeTrue();
            actual.ShouldBe(notification);
            target.Stop();            
        }

        [Test]
        public async Task Start_MultipleNotificationsReceived_SendsToBuffer()
        {
            // Arrange
            var notifications = new List<Notification>();
            var count = Dummy.CreateRandomInt(100);
            for (int i = 0; i < count; i++)
            {
                notifications.Add(
                    Dummy.CreateNotification(Event.Authorized));
            }
            var target = GetAndStartTarget();

            // Act
            foreach (var notification in notifications)
            {
                _producedNotifications.Add(notification);
            }

            await Task.Delay(count * 15);

            // Assert
            _notificationBufferBlock.Count.ShouldBe(count);

            Notification actual;
            while (_notificationBufferBlock.TryReceive(out actual))
            {
                notifications.ShouldContain(actual);
            }           
        }

        [Test]
        public async Task Start_CompletedNotificationBufferBlock_StopsConsumerTask()
        {
            // Arrange            
            var notification = Dummy.CreateNotification(Event.Authorized);
            var target = GetAndStartTarget();

            // Act
            _notificationBufferBlock.Complete();
            _producedNotifications.Add(notification);

            // Assert
            await target.NotificationListenerTask;
        }

        [Test]
        public async Task Start_CommandReceived_SendsToBuffer()
        {
            // Arrange            
            var command = Dummy.CreateCommand(Dummy.CreateTextContent());
            var target = GetAndStartTarget();

            // Act
            _producedCommands.Add(command);
            await Task.Delay(250);

            // Assert
            Command actual;
            _commandBufferBlock.TryReceive(out actual).ShouldBeTrue();
            actual.ShouldBe(command);
            target.Stop();
        }

        [Test]
        public async Task Start_MultipleCommandsReceived_SendsToBuffer()
        {
            // Arrange
            var commands = new List<Command>();
            var count = Dummy.CreateRandomInt(100);
            for (int i = 0; i < count; i++)
            {
                commands.Add(
                    Dummy.CreateCommand(Dummy.CreateTextContent()));
            }
            var target = GetAndStartTarget();

            // Act
            foreach (var command in commands)
            {
                _producedCommands.Add(command);
            }

            await Task.Delay(count * 15);

            // Assert
            _commandBufferBlock.Count.ShouldBe(count);

            Command actual;
            while (_commandBufferBlock.TryReceive(out actual))
            {
                commands.ShouldContain(actual);
            }
        }

        [Test]
        public async Task Start_CompletedCommandBufferBlock_StopsConsumerTask()
        {
            // Arrange            
            var command = Dummy.CreateCommand(Dummy.CreateTextContent());
            var target = GetAndStartTarget();

            // Act
            _commandBufferBlock.Complete();
            _producedCommands.Add(command);

            // Assert
            await target.CommandListenerTask;
        }
    }
}