import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AstronautDutyService } from './astronaut-duty.service';
import { App } from './app';
import { AstronautDutiesResponse } from './astronaut-duty.models';

describe('App', () => {
  let fixture: ComponentFixture<App>;
  let astronautDutyService: jasmine.SpyObj<AstronautDutyService>;

  const response: AstronautDutiesResponse = {
    success: true,
    message: 'Successful',
    responseCode: 200,
    person: {
      personId: 1,
      name: 'John Doe',
      currentRank: '1LT',
      currentDutyTitle: 'Commander',
      careerStartDate: '2026-10-01T00:00:00',
      careerEndDate: null,
    },
    astronautDuties: [
      {
        id: 1,
        personId: 1,
        rank: '1LT',
        dutyTitle: 'Commander',
        dutyStartDate: '2026-10-02T00:00:00',
        dutyEndDate: null,
      },
    ],
  };

  beforeEach(async () => {
    astronautDutyService = jasmine.createSpyObj<AstronautDutyService>(
      'AstronautDutyService',
      ['getByName'],
    );
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [{ provide: AstronautDutyService, useValue: astronautDutyService }],
    }).compileComponents();
    fixture = TestBed.createComponent(App);
  });

  it('renders the initial search experience', () => {
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Every mission');
    expect(compiled.querySelector('input[type="search"]')).toBeTruthy();
    expect(compiled.querySelector('.welcome-card')).toBeTruthy();
  });

  it('submits a name from the search form', async () => {
    astronautDutyService.getByName.and.returnValue(of(response));
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('input[type="search"]') as HTMLInputElement;
    input.value = 'John Doe';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    await fixture.whenStable();
    fixture.detectChanges();

    const submitButton = fixture.nativeElement.querySelector(
      'button[type="submit"]',
    ) as HTMLButtonElement;
    expect(submitButton.disabled).toBeFalse();

    submitButton.click();
    fixture.detectChanges();

    expect(astronautDutyService.getByName).toHaveBeenCalledWith('John Doe');
  });

  it('retrieves and renders the astronaut profile and duties', () => {
    astronautDutyService.getByName.and.returnValue(of(response));
    fixture.componentInstance.name = ' John Doe ';
    fixture.detectChanges();
    fixture.componentInstance.search();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(astronautDutyService.getByName).toHaveBeenCalledWith('John Doe');
    expect(compiled.querySelector('.profile-info h3')?.textContent).toContain('John Doe');
    expect(compiled.querySelector('.duty-title-line h3')?.textContent).toContain('Commander');
    expect(compiled.querySelector('.current-badge')?.textContent).toContain('CURRENT');
  });

  it('shows a no-record state when the API returns no person', () => {
    astronautDutyService.getByName.and.returnValue(
      of({ ...response, person: null, astronautDuties: [] }),
    );
    fixture.componentInstance.name = 'Unknown Person';
    fixture.detectChanges();
    fixture.componentInstance.search();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No personnel record found');
  });

  it('shows a useful error when the API cannot be reached', () => {
    astronautDutyService.getByName.and.returnValue(
      throwError(() => ({ status: 0 })),
    );
    fixture.componentInstance.name = 'John Doe';
    fixture.detectChanges();
    fixture.componentInstance.search();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Check that the Stargate API is running');
  });
});
