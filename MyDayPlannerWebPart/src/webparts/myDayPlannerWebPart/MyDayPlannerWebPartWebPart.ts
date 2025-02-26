import * as React from 'react';
import * as ReactDom from 'react-dom';
import { Version } from '@microsoft/sp-core-library';
import {
  type IPropertyPaneConfiguration,
  PropertyPaneTextField
} from '@microsoft/sp-property-pane';
import { BaseClientSideWebPart } from '@microsoft/sp-webpart-base';
import { IReadonlyTheme } from '@microsoft/sp-component-base';

import * as strings from 'MyDayPlannerWebPartWebPartStrings';
import MyDayPlannerWebPart from './components/MyDayPlannerWebPart';
import { IMyDayPlannerWebPartProps } from './components/IMyDayPlannerWebPartProps';
import { AadHttpClient} from '@microsoft/sp-http';

export interface IMyDayPlannerWebPartWebPartProps {
  description: string;
}

export default class MyDayPlannerWebPartWebPart extends BaseClientSideWebPart<IMyDayPlannerWebPartWebPartProps> {

  private _isDarkTheme: boolean = false;
  private _environmentMessage: string = '';
  private aadHttpClient: AadHttpClient;

  public async render(): Promise<void> {
    
    var httpResponse = await this.aadHttpClient.get(
      "http://localhost:7071/api/MyDayPlannerAgent",
      AadHttpClient.configurations.v1);
    
    var dayPlanneResult = await httpResponse.json();

    console.log(dayPlanneResult);

    const element: React.ReactElement<IMyDayPlannerWebPartProps> = React.createElement(
      MyDayPlannerWebPart,
      {
        description: this.properties.description,
        isDarkTheme: this._isDarkTheme,
        environmentMessage: this._environmentMessage,
        hasTeamsContext: !!this.context.sdks.microsoftTeams,
        userDisplayName: this.context.pageContext.user.displayName,
        dayPlanneResult:dayPlanneResult
      }
    );

    ReactDom.render(element, this.domElement);
  }

  protected onInit(): Promise<void> {
    
    return new Promise<void>((resolve: () => void, reject: (error: any) => void): void => {
      this.context.aadHttpClientFactory
        .getClient('41dc2ebf-8685-494d-adb2-2a3a5963cee9')
        .then((client: AadHttpClient): void => {
          this.aadHttpClient = client;
          resolve();
        }, err => reject(err));
    });
  }



  // private _getEnvironmentMessage(): Promise<string> {
  //   if (!!this.context.sdks.microsoftTeams) { // running in Teams, office.com or Outlook
  //     return this.context.sdks.microsoftTeams.teamsJs.app.getContext()
  //       .then(context => {
  //         let environmentMessage: string = '';
  //         switch (context.app.host.name) {
  //           case 'Office': // running in Office
  //             environmentMessage = this.context.isServedFromLocalhost ? strings.AppLocalEnvironmentOffice : strings.AppOfficeEnvironment;
  //             break;
  //           case 'Outlook': // running in Outlook
  //             environmentMessage = this.context.isServedFromLocalhost ? strings.AppLocalEnvironmentOutlook : strings.AppOutlookEnvironment;
  //             break;
  //           case 'Teams': // running in Teams
  //           case 'TeamsModern':
  //             environmentMessage = this.context.isServedFromLocalhost ? strings.AppLocalEnvironmentTeams : strings.AppTeamsTabEnvironment;
  //             break;
  //           default:
  //             environmentMessage = strings.UnknownEnvironment;
  //         }

  //         return environmentMessage;
  //       });
  //   }

  //   return Promise.resolve(this.context.isServedFromLocalhost ? strings.AppLocalEnvironmentSharePoint : strings.AppSharePointEnvironment);
  // }

  protected onThemeChanged(currentTheme: IReadonlyTheme | undefined): void {
    if (!currentTheme) {
      return;
    }

    this._isDarkTheme = !!currentTheme.isInverted;
    const {
      semanticColors
    } = currentTheme;

    if (semanticColors) {
      this.domElement.style.setProperty('--bodyText', semanticColors.bodyText || null);
      this.domElement.style.setProperty('--link', semanticColors.link || null);
      this.domElement.style.setProperty('--linkHovered', semanticColors.linkHovered || null);
    }

  }

  protected onDispose(): void {
    ReactDom.unmountComponentAtNode(this.domElement);
  }

  protected get dataVersion(): Version {
    return Version.parse('1.0');
  }

  protected getPropertyPaneConfiguration(): IPropertyPaneConfiguration {
    return {
      pages: [
        {
          header: {
            description: strings.PropertyPaneDescription
          },
          groups: [
            {
              groupName: strings.BasicGroupName,
              groupFields: [
                PropertyPaneTextField('description', {
                  label: strings.DescriptionFieldLabel
                })
              ]
            }
          ]
        }
      ]
    };
  }
}
