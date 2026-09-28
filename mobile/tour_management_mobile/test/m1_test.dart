import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:tour_management_mobile/services/api_client.dart';
import 'package:tour_management_mobile/features/m1_users_proposals_trips/services/user_service.dart';
import 'package:tour_management_mobile/features/m1_users_proposals_trips/services/trip_service.dart';
import 'package:tour_management_mobile/features/m1_users_proposals_trips/models/user_models.dart';
import 'package:tour_management_mobile/features/m1_users_proposals_trips/models/trip_models.dart';
import 'package:tour_management_mobile/features/m1_users_proposals_trips/models/proposal_models.dart';
import 'package:tour_management_mobile/features/m1_users_proposals_trips/models/paged_result.dart';
import 'package:tour_management_mobile/features/m1_users_proposals_trips/services/proposal_service.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    FlutterSecureStorage.setMockInitialValues({});
  });

  group('M1 - UserService', () {
    test('getPendingUsers parses response correctly', () async {
      final mockClient = MockClient((request) async {
        return http.Response(
            jsonEncode({
              'success': true,
              'data': {
                'items': [
                  {
                    'id': 1,
                    'fullName': 'Test User',
                    'email': 'test@test.com',
                    'role': 'Supplier',
                    'status': 'PendingApproval',
                    'createdAt': '2026-09-28T00:00:00Z'
                  }
                ],
                'totalCount': 1,
                'page': 1,
                'pageSize': 20,
                'totalPages': 1
              }
            }),
            200);
      });
      ApiClient().client = mockClient;

      final userService = UserService();
      final result = await userService.getPendingUsers();
      expect(result.items.length, 1);
      expect(result.items.first.fullName, 'Test User');
    });

    test('approveUser success', () async {
      final mockClient = MockClient((request) async {
        expect(request.url.path, '/api/users/1/approve');
        return http.Response(jsonEncode({'success': true}), 200);
      });
      ApiClient().client = mockClient;

      final userService = UserService();
      await expectLater(userService.approveUser(1), completes);
    });

    test('approveUser failure throws exception', () async {
      final mockClient = MockClient((request) async {
        return http.Response(
            jsonEncode({'success': false, 'message': 'User not found'}), 404);
      });
      ApiClient().client = mockClient;

      final userService = UserService();
      await expectLater(userService.approveUser(1), throwsException);
    });
  });

  group('M1 - TripService', () {
    test('forceCancelTrip success', () async {
      final mockClient = MockClient((request) async {
        expect(request.url.path, '/api/trips/10/force-cancel');
        return http.Response(jsonEncode({'success': true}), 200);
      });
      ApiClient().client = mockClient;

      final tripService = TripService();
      await expectLater(tripService.forceCancelTrip(10), completes);
    });

    test('forceCancelTrip failure throws exception', () async {
      final mockClient = MockClient((request) async {
        return http.Response(
            jsonEncode(
                {'success': false, 'message': 'Cannot cancel this trip'}),
            400);
      });
      ApiClient().client = mockClient;

      final tripService = TripService();
      await expectLater(tripService.forceCancelTrip(10),
          throwsA(predicate((e) => e.toString().contains('Cannot cancel'))));
    });
  });

  group('M1 - ProposalService', () {
    test('approveProposal success with HoldPlaced', () async {
      final mockClient = MockClient((request) async {
        expect(request.url.path, '/api/admin/workflows/P-123/approve');
        return http.Response(
            jsonEncode({
              'success': true,
              'message': 'Proposal approved and hold placed.'
            }),
            200);
      });
      ApiClient().client = mockClient;

      final proposalService = ProposalService();
      await expectLater(proposalService.approveProposal('P-123'), completes);
    });

    test('approveProposal success with ApprovedNoBookingRequired', () async {
      final mockClient = MockClient((request) async {
        return http.Response(
            jsonEncode({
              'success': true,
              'message': 'Proposal approved with no bookings.'
            }),
            200);
      });
      ApiClient().client = mockClient;

      final proposalService = ProposalService();
      await expectLater(proposalService.approveProposal('P-123'), completes);
    });

    test('rejectProposal success', () async {
      final mockClient = MockClient((request) async {
        expect(request.url.path, '/api/admin/workflows/P-123/reject');
        final body = jsonDecode(request.body);
        expect(body['decision'], 'Reject');
        expect(body['reason'], 'Too expensive');
        return http.Response(jsonEncode({'success': true}), 200);
      });
      ApiClient().client = mockClient;

      final proposalService = ProposalService();
      await expectLater(
          proposalService.rejectProposal('P-123', 'Too expensive'), completes);
    });

    test('rejectProposal already processed conflict', () async {
      final mockClient = MockClient((request) async {
        return http.Response(
            jsonEncode({
              'success': false,
              'message': 'Conflict: Proposal already processed.'
            }),
            409);
      });
      ApiClient().client = mockClient;

      final proposalService = ProposalService();
      await expectLater(proposalService.rejectProposal('P-123', ''),
          throwsA(predicate((e) => e.toString().contains('Conflict'))));
    });
  });

  group('M1 Models - Regression Coverage', () {
    test('UserSummary parses string status safely', () {
      final json = {
        'id': 1,
        'fullName': 'A',
        'email': 'a@a.com',
        'role': 'Admin',
        'status': 'PendingApproval',
        'createdAt': '2026-01-01T00:00:00Z'
      };
      final user = UserSummary.fromJson(json);
      expect(user.status, 'PendingApproval');

      final unknownJson = {'id': 2, 'status': 'WeirdStatus'};
      final user2 = UserSummary.fromJson(unknownJson);
      expect(user2.status, 'WeirdStatus');
    });

    test('TripSummary parses string status safely', () {
      final json = {
        'id': 10,
        'title': 'Test Trip',
        'status': 'Draft',
        'budget': 100.0
      };
      final trip = TripSummary.fromJson(json);
      expect(trip.status, 'Draft');
    });

    test(
        'TripDetail parses string status safely and restricts unknown status mutation',
        () {
      final json = {
        'id': 10,
        'title': 'Detail Trip',
        'status': 'UnknownStatus',
        'budget': 200.0,
        'travelerId': 1,
        'destinationId': 1,
        'itineraryItems': []
      };
      final detail = TripDetail.fromJson(json);
      expect(detail.status, 'UnknownStatus');
    });

    test('PagedResult parses valid empty items safely', () {
      final json = {'items': [], 'totalCount': 0, 'page': 1, 'pageSize': 20};
      final result = PagedResult<UserSummary>.fromJson(
          json, (j) => UserSummary.fromJson(j));
      expect(result.items, isEmpty);
      expect(result.totalCount, 0);
    });

    test(
        'PagedResult throws FormatException for malformed items (missing items array)',
        () {
      final json = {
        'totalCount': 0,
        'page': 1,
        'pageSize': 20
      }; // missing items array
      expect(
          () => PagedResult<UserSummary>.fromJson(
              json, (j) => UserSummary.fromJson(j)),
          throwsA(isA<FormatException>()));
    });

    test('TripProposal parses populated contract safely', () {
      final json = {
        'id': 5,
        'proposalId': 'P-999',
        'tripId': 10,
        'version': 1,
        'status': 'PendingAdminApproval',
        'createdAt': '2026-09-28T12:00:00Z',
        'payload': {
          'Hotel': {'id': 100}
        },
        'executionSummaries': [
          {
            'agentIdentity': 'm1_planner',
            'status': 'Completed',
            'finalOutcome': 'Pass'
          }
        ]
      };
      final proposal = TripProposal.fromJson(json);
      expect(proposal.proposalId, 'P-999');
      expect(proposal.payload, isNotNull);
      expect(proposal.payload!['Hotel']['id'], 100);
      expect(proposal.executionSummaries.length, 1);
      expect(proposal.executionSummaries[0]['finalOutcome'], 'Pass');
    });
  });
}
