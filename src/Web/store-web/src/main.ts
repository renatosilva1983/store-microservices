import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { bootstrapApplication } from '@angular/platform-browser';
import { App } from './app/app';
import { appConfig } from './app/app.config';

// Formatação de moeda e números em pt-BR (R$ 1.234,56).
registerLocaleData(localePt);

bootstrapApplication(App, appConfig).catch((err) => console.error(err));
