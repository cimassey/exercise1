import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { AstronautDuty, AstronautDutiesResponse, AstronautPerson } from './astronaut-duty.models';
import { AstronautDutyService } from './astronaut-duty.service';

@Component({
  selector: 'app-root',
  imports: [DatePipe],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly astronautDutyService = inject(AstronautDutyService);

  name = '';
  protected loading = false;
  protected searched = false;
  protected errorMessage = '';
  protected person: AstronautPerson | null = null;
  protected duties: AstronautDuty[] = [];

  protected get hasCurrentDuty(): boolean {
    return this.duties.some((duty) => duty.dutyEndDate === null);
  }

  protected get isRetired(): boolean {
    return this.person?.currentDutyTitle?.toUpperCase() === 'RETIRED';
  }

  protected updateName(event: Event): void {
    this.name = (event.target as HTMLInputElement).value;
    this.errorMessage = '';
  }

  protected onSubmit(event: Event): void {
    event.preventDefault();
    this.search();
  }

  search(): void {
    const name = this.name.trim();
    if (!name || this.loading) {
      return;
    }

    this.loading = true;
    this.searched = true;
    this.errorMessage = '';
    this.person = null;
    this.duties = [];

    this.astronautDutyService.getByName(name).subscribe({
      next: (response: AstronautDutiesResponse) => {
        this.person = response.person;
        this.duties = response.astronautDuties ?? [];
        this.loading = false;
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage = this.getErrorMessage(error);
        this.loading = false;
      },
    });
  }

  protected searchFor(name: string): void {
    this.name = name;
    this.search();
  }

  protected clearSearch(): void {
    this.name = '';
    this.person = null;
    this.duties = [];
    this.errorMessage = '';
    this.searched = false;
  }

  protected initials(name: string): string {
    return name
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => part.charAt(0))
      .join('')
      .toUpperCase();
  }

  private getErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return 'The archive could not be reached. Check that the Stargate API is running, then try again.';
    }

    if (error.status >= 500) {
      return 'The archive encountered a problem while retrieving this record. Please try again.';
    }

    return error.error?.message ?? 'The request could not be completed. Please check the name and try again.';
  }
}
