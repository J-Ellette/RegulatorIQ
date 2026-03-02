import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export interface AppNotification {
  message: string;
  severity: 'success' | 'info' | 'warning' | 'error';
  frameworkId?: string;
}

type NotificationHandler = (notification: AppNotification) => void;

let connection: HubConnection | null = null;
const handlers = new Set<NotificationHandler>();

const notifyAll = (notification: AppNotification) => {
  handlers.forEach((handler) => handler(notification));
};

export const subscribeToNotifications = (handler: NotificationHandler) => {
  handlers.add(handler);
  return () => handlers.delete(handler);
};

export const connectNotifications = async () => {
  if (connection) {
    return;
  }

  connection = new HubConnectionBuilder()
    .withUrl('/hubs/notifications')
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();

  connection.on('impactAssessmentCompleted', (payload: any) => {
    const title = payload?.documentTitle || 'regulatory document';
    notifyAll({
      message: `Impact assessment completed for ${title}.`,
      severity: 'success',
      frameworkId: payload?.frameworkId,
    });
  });

  connection.on('frameworkSynced', (payload: any) => {
    const frameworkName = payload?.frameworkName || 'Framework';
    const count = payload?.newRegulationsFound ?? 0;
    notifyAll({
      message: `${frameworkName} synced. ${count} new regulation${count === 1 ? '' : 's'} found.`,
      severity: 'info',
      frameworkId: payload?.frameworkId,
    });
  });

  connection.on('alertAcknowledged', (payload: any) => {
    const title = payload?.title || 'Alert';
    notifyAll({
      message: `${title} was acknowledged.`,
      severity: 'success',
      frameworkId: payload?.frameworkId,
    });
  });

  connection.on('alertResolved', (payload: any) => {
    const title = payload?.title || 'Alert';
    notifyAll({
      message: `${title} was resolved.`,
      severity: 'success',
      frameworkId: payload?.frameworkId,
    });
  });

  connection.on('frameworkLifecycleUpdated', (payload: any) => {
    const frameworkName = payload?.frameworkName || 'Framework';
    notifyAll({
      message: `${frameworkName} lifecycle updated to ${payload?.status || 'active'}.`,
      severity: 'info',
      frameworkId: payload?.frameworkId,
    });
  });

  try {
    await connection.start();
  } catch (error) {
    console.error('Failed to connect notifications hub', error);
  }
};
