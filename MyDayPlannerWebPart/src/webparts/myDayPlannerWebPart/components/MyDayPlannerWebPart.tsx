import * as React from 'react';
import styles from './MyDayPlannerWebPart.module.scss';
import type { IMyDayPlannerWebPartProps } from './IMyDayPlannerWebPartProps';

export default class MyDayPlannerWebPart extends React.Component<IMyDayPlannerWebPartProps> {
  public render(): React.ReactElement<IMyDayPlannerWebPartProps> {
    const {
      isDarkTheme,
      hasTeamsContext,
      dayPlanneResult
    } = this.props;

    return (
      <section className={`${styles.myDayPlannerWebPart} ${hasTeamsContext ? styles.teams : ''}`}>
        <div className={styles.welcome}>
          <img alt="" src={isDarkTheme ? require('../assets/welcome-dark.png') : require('../assets/welcome-light.png')} className={styles.welcomeImage} />
          <h2>My Day Planner</h2>
        </div>
        <div>
          <h3>Today's Meetings</h3>
            <ul>
            {dayPlanneResult.meetings.map((meeting: { meetingTitle: string; startTime: string; endTime: string; attendees: string; meetingSummary: string; preparationRecommendation: string; }, index: number) => (
              (<li key={index}>
              <h4>{meeting.meetingTitle}</h4>
              <p><strong>Start Time:</strong> {meeting.startTime}</p>
              <p><strong>End Time:</strong> {meeting.endTime}</p>
              <p><strong>Attendees:</strong> {meeting.attendees}</p>
              <p><strong>Meeting Summary:</strong> {meeting.meetingSummary}</p>
              <p><strong>Preparation Recommendation:</strong> {meeting.preparationRecommendation}</p>
              </li>)
            ))}
            </ul>
        </div>
      </section>
    );
  }
}
