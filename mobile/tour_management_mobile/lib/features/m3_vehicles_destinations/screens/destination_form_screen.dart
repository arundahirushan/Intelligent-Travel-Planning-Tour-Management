import 'package:flutter/material.dart';
import '../models/destination_response_dto.dart';
import '../services/destination_service.dart';

// Create or Edit destination form.
// When [existing] is null, creates a new destination (POST /api/destinations).
// When [existing] is provided, updates it (PUT /api/destinations/{id}).
//
// Fields match CreateDestinationDto / UpdateDestinationDto exactly:
//   - name (required)
//   - region (required)
//   - description (required)
//   - imageUrl (optional URL string)
//
// Coordinates (latitude/longitude) are intentionally absent because neither
// CreateDestinationDto nor UpdateDestinationDto currently accepts them.
class DestinationFormScreen extends StatefulWidget {
  final DestinationResponseDto? existing;

  const DestinationFormScreen({super.key, this.existing});

  @override
  State<DestinationFormScreen> createState() => _DestinationFormScreenState();
}

class _DestinationFormScreenState extends State<DestinationFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final DestinationService _service = DestinationService();

  late final TextEditingController _nameController;
  late final TextEditingController _regionController;
  late final TextEditingController _descriptionController;
  late final TextEditingController _imageUrlController;

  bool _isSubmitting = false;

  bool get _isEditing => widget.existing != null;

  @override
  void initState() {
    super.initState();
    final d = widget.existing;
    _nameController = TextEditingController(text: d?.name ?? '');
    _regionController = TextEditingController(text: d?.region ?? '');
    _descriptionController = TextEditingController(text: d?.description ?? '');
    _imageUrlController = TextEditingController(text: d?.imageUrl ?? '');
  }

  @override
  void dispose() {
    _nameController.dispose();
    _regionController.dispose();
    _descriptionController.dispose();
    _imageUrlController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_isSubmitting) return;
    if (!_formKey.currentState!.validate()) return;

    setState(() => _isSubmitting = true);

    final name = _nameController.text.trim();
    final region = _regionController.text.trim();
    final description = _descriptionController.text.trim();
    final imageUrl = _imageUrlController.text.trim();

    try {
      if (_isEditing) {
        await _service.updateDestination(
          id: widget.existing!.id,
          name: name,
          region: region,
          description: description,
          imageUrl: imageUrl.isEmpty ? null : imageUrl,
        );
      } else {
        await _service.createDestination(
          name: name,
          region: region,
          description: description,
          imageUrl: imageUrl.isEmpty ? null : imageUrl,
        );
      }

      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            _isEditing
                ? 'Destination updated successfully.'
                : 'Destination created successfully.',
          ),
        ),
      );
      // Return true so the calling screen knows to refresh.
      Navigator.pop(context, true);
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(e.toString().replaceAll('Exception: ', '')),
          backgroundColor: Colors.red,
        ),
      );
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title:
            Text(_isEditing ? 'Edit Destination' : 'Create Destination'),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              TextFormField(
                controller: _nameController,
                decoration: const InputDecoration(
                  labelText: 'Name *',
                  border: OutlineInputBorder(),
                ),
                textInputAction: TextInputAction.next,
                validator: (v) =>
                    (v == null || v.trim().isEmpty) ? 'Name is required.' : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _regionController,
                decoration: const InputDecoration(
                  labelText: 'Region *',
                  hintText: 'e.g. Southern Province, Hill Country',
                  border: OutlineInputBorder(),
                ),
                textInputAction: TextInputAction.next,
                validator: (v) => (v == null || v.trim().isEmpty)
                    ? 'Region is required.'
                    : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _descriptionController,
                decoration: const InputDecoration(
                  labelText: 'Description *',
                  border: OutlineInputBorder(),
                ),
                maxLines: 4,
                textInputAction: TextInputAction.next,
                validator: (v) => (v == null || v.trim().isEmpty)
                    ? 'Description is required.'
                    : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _imageUrlController,
                decoration: const InputDecoration(
                  labelText: 'Image URL (optional)',
                  hintText: 'https://...',
                  border: OutlineInputBorder(),
                ),
                keyboardType: TextInputType.url,
                textInputAction: TextInputAction.done,
                onFieldSubmitted: (_) => _submit(),
              ),
              const SizedBox(height: 24),
              ElevatedButton(
                onPressed: _isSubmitting ? null : _submit,
                style: ElevatedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 14),
                ),
                child: _isSubmitting
                    ? const SizedBox(
                        height: 20,
                        width: 20,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : Text(
                        _isEditing ? 'Save Changes' : 'Create Destination',
                        style: const TextStyle(fontSize: 16),
                      ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

