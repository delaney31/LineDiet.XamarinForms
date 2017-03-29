﻿using LineDietXF.Enumerations;
using LineDietXF.Helpers;
using LineDietXF.Interfaces;
using LineDietXF.Types;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services;
using System;

namespace LineDietXF.ViewModels
{
    /// <summary>
    /// Set a goal page shown modally from first tab of app or main menu
    /// </summary>
    public class SetGoalPageViewModel : BaseViewModel, INavigationAware
    {
        // Bindable Properties
        DateTime _startDate = DateTime.Today.Date;
        public DateTime StartDate
        {
            get { return _startDate; }
            set
            {
                SetProperty(ref _startDate, value);
                UpdateStartWeightFromStartDate();
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        string _startWeight;
        public string StartWeight
        {
            get { return _startWeight; }
            set
            {
                SetProperty(ref _startWeight, value);
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        string _startWeightStones;
        public string StartWeightStones
        {
            get { return _startWeightStones; }
            set
            {
                SetProperty(ref _startWeightStones, value);
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        string _startWeightStonePounds;
        public string StartWeightStonePounds
        {
            get { return _startWeightStonePounds; }
            set
            {
                SetProperty(ref _startWeightStonePounds, value);
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        string _goalWeightStones;
        public string GoalWeightStones
        {
            get { return _goalWeightStones; }
            set
            {
                SetProperty(ref _goalWeightStones, value);
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        string _goalWeightStonePounds;
        public string GoalWeightStonePounds
        {
            get { return _goalWeightStonePounds; }
            set
            {
                SetProperty(ref _goalWeightStonePounds, value);
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        DateTime _goalDate = DateTime.Today.AddMonths(Constants.App.SetGoalPage_DefaultGoalDateOffsetInMonths);
        public DateTime GoalDate
        {
            get { return _goalDate; }
            set
            {
                SetProperty(ref _goalDate, value);
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        string _goalWeight;
        public string GoalWeight
        {
            get { return _goalWeight; }
            set
            {
                SetProperty(ref _goalWeight, value);
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        bool _showPoundsEntryFields;
        public bool ShowStonesEntryFields
        {
            get { return _showPoundsEntryFields; }
            set { SetProperty(ref _showPoundsEntryFields, value); }
        }

        // Services
        IDataService DataService { get; set; }
        ISettingsService SettingsService { get; set; }

        // Commands
        public DelegateCommand SaveCommand { get; set; }
        public DelegateCommand CloseCommand { get; set; }

        public SetGoalPageViewModel(INavigationService navigationService, IAnalyticsService analyticsService, ISettingsService settingsService, IPageDialogService dialogService, IDataService dataService) :
            base(navigationService, analyticsService, dialogService)
        {
            // Store off injected services
            DataService = dataService;
            SettingsService = settingsService;

            // Setup bindable commands
            SaveCommand = new DelegateCommand(Save, SaveCanExecute);
            CloseCommand = new DelegateCommand(Close);
        }

        public void OnNavigatingTo(NavigationParameters parameters)
        {
            ShowStonesEntryFields = SettingsService.WeightUnit == WeightUnitEnum.StonesAndPounds;
            UpdateStartWeightFromStartDate();
        }

        public void OnNavigatedTo(NavigationParameters parameters)
        {
            AnalyticsService.TrackPageView(Constants.Analytics.Page_SetGoal);

            TryLoadExistingGoal();
        }

        public void OnNavigatedFrom(NavigationParameters parameters) { }

        async void TryLoadExistingGoal()
        {
            WeightLossGoal existingGoal;
            try
            {
                IncrementPendingRequestCount(); // show loading

                existingGoal = await DataService.GetGoal();
            }
            catch (Exception ex)
            {
                AnalyticsService.TrackFatalError($"{nameof(TryLoadExistingGoal)} - an exception occurred.", ex);
                // NOTE:: not showing an error here as this is not in response to user action. potentially should show a non-intrusive error banner
                return;
            }
            finally
            {
                DecrementPendingRequestCount(); // hide loading
            }

            if (existingGoal == null)
                return;

            // NOTE:: we could see if the goal date was already past and not load it in that case, but it would be better to still
            // bring in what they had before and just let them update it (ex: moving goal date forward / back)
            StartDate = existingGoal.StartDate;            
            GoalDate = existingGoal.GoalDate;

            // setup entry fields for start and goal weights (we have different fields for stones (2 fields) than kg/pounds)
            if (ShowStonesEntryFields)
            {
                var startWeightStones = WeightLogicHelpers.ConvertPoundsToStonesAndPounds(existingGoal.StartWeight);
                var goalWeightStones = WeightLogicHelpers.ConvertPoundsToStonesAndPounds(existingGoal.GoalWeight);

                StartWeightStones = startWeightStones.Item1.ToString();
                StartWeightStonePounds = string.Format(Constants.Strings.Common_WeightFormat, startWeightStones.Item2);

                GoalWeightStones = goalWeightStones.Item1.ToString();
                GoalWeightStonePounds = string.Format(Constants.Strings.Common_WeightFormat, goalWeightStones.Item2);
            }
            else
            {
                StartWeight = string.Format(Constants.Strings.Common_WeightFormat, existingGoal.StartWeight);
                GoalWeight = string.Format(Constants.Strings.Common_WeightFormat, existingGoal.GoalWeight);
            }
        }

        async void UpdateStartWeightFromStartDate()
        {
            // pre-populate today's weight field if it has a value
            try
            {
                IncrementPendingRequestCount();

                var existingStartDateWeight = await DataService.GetWeightEntryForDate(StartDate);
                if (existingStartDateWeight != null)
                {
                    if (ShowStonesEntryFields)
                    {
                        var startWeightStones = WeightLogicHelpers.ConvertPoundsToStonesAndPounds(existingStartDateWeight.Weight);
                        StartWeightStones = startWeightStones.Item1.ToString();
                        StartWeightStonePounds = string.Format(Constants.Strings.Common_WeightFormat, startWeightStones.Item2);
                    }
                    else
                    {
                        StartWeight = existingStartDateWeight.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                AnalyticsService.TrackFatalError($"{nameof(UpdateStartWeightFromStartDate)} - an exception occurred.", ex);
                // NOTE:: not showing an error here as this is not in response to user action. potentially should show a non-intrusive error banner
            }
            finally
            {
                DecrementPendingRequestCount();
            }
        }

        bool SaveCanExecute()
        {
            // disable the save button if their goal date is before their start date or the dates are equal
            if (GoalDate <= StartDate)
                return false;

            if (ShowStonesEntryFields) // logic for pounds entry (two fields)
            {
                // disable the save button if any weight field is empty
                if (string.IsNullOrWhiteSpace(StartWeightStones) || string.IsNullOrWhiteSpace(StartWeightStonePounds) ||
                    string.IsNullOrWhiteSpace(GoalWeightStones) || string.IsNullOrWhiteSpace(GoalWeightStonePounds))
                    return false;

                // disable the save button if weight fields can't be parsed
                if (GetStartWeightInStones() == null || GetGoalWeightInStones() == null)
                    return false;
            }
            else // logic for single field (kg or pounds)
            { 
                // disable the save button if either weight field is empty
                if (string.IsNullOrWhiteSpace(StartWeight) || string.IsNullOrWhiteSpace(GoalWeight))
                    return false;

                // disable the save button if either weight text field can't be parsed
                decimal startWeight, goalWeight;
                if (!decimal.TryParse(StartWeight, out startWeight) || !decimal.TryParse(GoalWeight, out goalWeight))
                    return false;
            }

            return true;
        }

        Tuple<int, decimal> GetStartWeightInStones()
        {            
            int startWeightStones;
            if (!int.TryParse(StartWeightStones, out startWeightStones))
                return null;

            decimal startWeightPounds;
            if (!decimal.TryParse(StartWeightStonePounds, out startWeightPounds))
                return null;

            return new Tuple<int, decimal>(startWeightStones, startWeightPounds);
        }

        Tuple<int, decimal> GetGoalWeightInStones()
        {
            int goalWeightStones;
            if (!int.TryParse(GoalWeightStones, out goalWeightStones))
                return null;

            decimal goalWeightPounds;
            if (!decimal.TryParse(GoalWeightStonePounds, out goalWeightPounds))
                return null;

            return new Tuple<int, decimal>(goalWeightStones, goalWeightPounds);
        }

        async void Save()
        {
            AnalyticsService.TrackEvent(Constants.Analytics.SetGoalCategory, Constants.Analytics.SetGoal_SavedGoal, 1);

            // convert entered value to a valid weight
            bool parsedWeightFields = true;
            decimal startWeight = 0;
            decimal goalWeight = 0;

            if (ShowStonesEntryFields)
            {
                var startWeightStoneFields = GetStartWeightInStones();
                var goalWeightStoneFields = GetGoalWeightInStones();

                if (startWeightStoneFields == null || goalWeightStoneFields == null)
                    parsedWeightFields = false;

                startWeight = WeightLogicHelpers.ConvertStonesToDecimal(startWeightStoneFields);
                goalWeight = WeightLogicHelpers.ConvertStonesToDecimal(goalWeightStoneFields);
            }
            else
            {
                if (!decimal.TryParse(StartWeight, out startWeight) || !decimal.TryParse(GoalWeight, out goalWeight))
                    parsedWeightFields = false;                
            }

            if (!parsedWeightFields)
            {
                // show error about invalid value if we can't convert the entered value to a decimal
                await DialogService.DisplayAlertAsync(Constants.Strings.SetGoalPage_InvalidWeight_Title,
                    Constants.Strings.SetGoalPage_InvalidWeight_Message,
                    Constants.Strings.GENERIC_OK);

                return;
            }

            // give warning if goal weight is greater than start weight
            // NOTE:: we don't prevent this scenario as I have had friends intentionally use the previous version of line diet for
            // tracking weight gain during pregnancy or muscle building - so we just give a warning. We also don't prevent equal
            // start and goal weights in case they just want a line to show a maintenance weight they are trying to stay at
            if (goalWeight > startWeight)
            {
                await DialogService.DisplayAlertAsync(Constants.Strings.SetGoalPage_GoalWeightGreaterThanStartWeight_Title,
                    Constants.Strings.SetGoalpage_GoalWeightGreaterThanStartWeight_Message,
                    Constants.Strings.GENERIC_OK);
            }

            try
            {
                IncrementPendingRequestCount();

                // see if they've entered a different weight already for this date, if so warn them about it being updated
                var existingWeight = await DataService.GetWeightEntryForDate(StartDate);
                if (existingWeight != null)
                {
                    if (existingWeight.Weight != startWeight)
                    {
                        // show different message for stones/pounds
                        string warningMessage;
                        if (ShowStonesEntryFields)
                        {
                            var existingWeightInStones = WeightLogicHelpers.ConvertPoundsToStonesAndPounds(existingWeight.Weight);
                            var startWeightInStones = WeightLogicHelpers.ConvertPoundsToStonesAndPounds(startWeight);

                            warningMessage = string.Format(Constants.Strings.Common_UpdateExistingWeight_Message, 
                                string.Format(Constants.Strings.Common_Stones_WeightFormat, existingWeightInStones.Item1, existingWeightInStones.Item2),
                                StartDate, 
                                string.Format(Constants.Strings.Common_Stones_WeightFormat, startWeightInStones.Item1, startWeightInStones.Item2));
                        }
                        else
                        {
                            warningMessage = string.Format(Constants.Strings.Common_UpdateExistingWeight_Message, existingWeight.Weight, StartDate, startWeight);
                        }

                        // show warning that an existing entry will be updated (is actually deleted and re-added), allow them to cancel
                        var result = await DialogService.DisplayAlertAsync(Constants.Strings.Common_UpdateExistingWeight_Title, warningMessage,
                            Constants.Strings.GENERIC_OK,
                            Constants.Strings.GENERIC_CANCEL);

                        // if they canceled the dialog then return without changing anything
                        if (!result)
                            return;
                    }

                    // remove existing weight
                    if (!await DataService.RemoveWeightEntryForDate(StartDate))
                    {
                        AnalyticsService.TrackError($"{nameof(Save)} - Error when trying to remove existing weight entry for start date");

                        await DialogService.DisplayAlertAsync(Constants.Strings.Common_SaveError,
                            Constants.Strings.SetGoalPage_Save_RemoveExistingWeightFailed_Message, Constants.Strings.GENERIC_OK);
                        return;
                    }
                }

                var addStartWeightResult = await DataService.AddWeightEntry(new WeightEntry(StartDate, startWeight, SettingsService.WeightUnit));
                if (!addStartWeightResult)
                {
                    AnalyticsService.TrackError($"{nameof(Save)} - Error when trying to add weight entry for start date");

                    await DialogService.DisplayAlertAsync(Constants.Strings.Common_SaveError,
                        Constants.Strings.SetGoalPage_Save_AddingWeightFailed_Message, Constants.Strings.GENERIC_OK);
                    return;
                }

                var weightLossGoal = new WeightLossGoal(StartDate, startWeight, GoalDate.Date, goalWeight, SettingsService.WeightUnit);
                if (!await DataService.SetGoal(weightLossGoal))
                {
                    AnalyticsService.TrackError($"{nameof(Save)} - Error when trying to save new weight loss goal");

                    await DialogService.DisplayAlertAsync(Constants.Strings.Common_SaveError,
                        Constants.Strings.SetGoalPage_Save_AddingGoalFailed_Message, Constants.Strings.GENERIC_OK);
                    return;
                }

                await NavigationService.GoBackAsync(useModalNavigation: true);
            }
            catch (Exception ex)
            {
                AnalyticsService.TrackFatalError($"{nameof(Save)} - an exception occurred.", ex);

                await DialogService.DisplayAlertAsync(Constants.Strings.Common_SaveError,
                    Constants.Strings.SetGoalPage_Save_Exception_Message, Constants.Strings.GENERIC_OK);
            }
            finally
            {
                DecrementPendingRequestCount();
            }
        }

        void Close()
        {
            NavigationService.GoBackAsync(useModalNavigation: true);
        }
    }
}
